using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;
using MediatR;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;

namespace Joseco.Outbox.EFCore.Procesor;

public class OutboxProcessor<E>(
    IOutboxRepository<E> outboxRepository,
    IOutboxDatabase<E> outboxDatabase,
    IPublisher publisher)
{
    private static readonly ActivitySource ActivitySource = new("Joseco.Outbox");

    private static readonly Meter Meter = new("Joseco.Outbox");

    private static readonly Counter<long> MessagesProcessedCounter = Meter.CreateCounter<long>("outbox.messages.processed",
        description: "Outbox messages handled; outcome = published | failed.");
    private static readonly Histogram<double> PublishDuration = Meter.CreateHistogram<double>("outbox.message.publish.duration",
        unit: "s", description: "Time to publish one outbox message and mark it processed.");
    private static readonly Histogram<double> PendingTime = Meter.CreateHistogram<double>("outbox.message.delay",
        unit: "s", description: "Time a message waited in the outbox between being created and being published.");

    // Backlog seen by the last polling cycle (per closed generic type, i.e. per DbContext of the service).
    private static int _lastBatchSize;
    private static double _lastOldestAgeSeconds;

    static OutboxProcessor()
    {
        Meter.CreateObservableGauge("outbox.unprocessed.batch_size", () => _lastBatchSize,
            description: "Unprocessed messages read by the last polling cycle (capped by the page size).");
        Meter.CreateObservableGauge("outbox.unprocessed.oldest_age", () => _lastOldestAgeSeconds,
            unit: "s", description: "Age of the oldest unprocessed message seen by the last polling cycle; 0 when the outbox is empty.");
    }

    public async Task Process(CancellationToken cancellationToken)
    {
        var messages = (await outboxRepository.GetUnprocessedAsync()).ToList();

        _lastBatchSize = messages.Count;
        _lastOldestAgeSeconds = messages.Count == 0
            ? 0
            : Math.Max(0, (DateTime.UtcNow - messages.Min(m => m.Created)).TotalSeconds);

        foreach (var item in messages)
        {

            // Opentelemetry instrumentation

            string traceIdStr = item.TraceId!;
            string spanIdStr = item.SpanId!;

            ActivityContext parentContext;

            try
            {
                var traceId = ActivityTraceId.CreateFromString(traceIdStr.AsSpan());
                var spanId = ActivitySpanId.CreateFromString(spanIdStr.AsSpan());

                parentContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            }
            catch
            {
                // fallback si los valores son inválidos
                parentContext = default;
            }

            using var activity = ActivitySource.StartActivity("OutboxProcesor", ActivityKind.Producer, parentContext);

            if (item == null || item.Content is null)
            {
                continue;
            }

            Type type = typeof(OutboxMessage<>)
                   .MakeGenericType(item.Content.GetType());

            if (type is null)
            {
                continue;
            }

            string messageType = item.Type;
            long started = Stopwatch.GetTimestamp();

            try
            {
                var confirmedEvent = (INotification)Activator
                        .CreateInstance(type, item.Content);

                await publisher.Publish(confirmedEvent);

                item.MarkAsProcessed();

                await outboxRepository.Update(item);

                await outboxDatabase.CommitAsync(cancellationToken);
            }
            catch
            {
                MessagesProcessedCounter.Add(1, new TagList { { "message_type", messageType }, { "outcome", "failed" } });
                throw;
            }

            MessagesProcessedCounter.Add(1, new TagList { { "message_type", messageType }, { "outcome", "published" } });
            PublishDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object?>("message_type", messageType));
            PendingTime.Record(Math.Max(0, (item.ProcessedOn!.Value - item.Created).TotalSeconds),
                new KeyValuePair<string, object?>("message_type", messageType));
        }
    }
}
