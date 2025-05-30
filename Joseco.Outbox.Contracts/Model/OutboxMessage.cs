using MediatR;

namespace Joseco.Outbox.Contracts.Model;

public class OutboxMessage<E> : INotification
{

    public E Content { get; private set; }

    public Guid Id { get; set; }

    public string Type { get; set; }

    public DateTime Created { get; set; }

    public bool Processed { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public string? CorrelationId { get; set; }

    public string? TraceId { get; set; }
    public string? SpanId { get; set; }

    public OutboxMessage(E content) : this(content, null, null, null)
    {
        Id = Guid.NewGuid();
        Created = DateTime.Now.ToUniversalTime();
        Processed = false;
        Content = content;
        Type = content.GetType().Name;
        CorrelationId = null;
    }

    public OutboxMessage(E content, string? correlationId, string? traceId, string? spanId)
    {
        Id = Guid.NewGuid();
        Created = DateTime.Now.ToUniversalTime();
        Processed = false;
        Content = content;
        Type = content.GetType().Name;
        CorrelationId = correlationId;
        TraceId = traceId;
        SpanId = spanId;
    }

    public void MarkAsProcessed()
    {
        ProcessedOn = DateTime.Now.ToUniversalTime();
        Processed = true;
    }

    private OutboxMessage()
    {

    }


}
