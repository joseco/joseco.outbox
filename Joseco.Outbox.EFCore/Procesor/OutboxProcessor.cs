using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;
using MediatR;
using System.Diagnostics;
using OpenTelemetry;

namespace Joseco.Outbox.EFCore.Procesor;

public class OutboxProcessor<E>(
    IOutboxRepository<E> outboxRepository,
    IOutboxDatabase<E> outboxDatabase,
    IPublisher publisher)
{
    private static readonly ActivitySource ActivitySource = new("Joseco.Outbox");

    public async Task Process(CancellationToken cancellationToken)
    {
        IEnumerable<OutboxMessage<E>> messages = await outboxRepository.GetUnprocessedAsync();

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

            var confirmedEvent = (INotification)Activator
                    .CreateInstance(type, item.Content);

            await publisher.Publish(confirmedEvent);

            item.MarkAsProcessed();

            await outboxRepository.Update(item);

            await outboxDatabase.CommitAsync(cancellationToken);
        }
    }
}
