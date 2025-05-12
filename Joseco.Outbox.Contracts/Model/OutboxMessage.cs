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

    public OutboxMessage(E content, string? correlationId = null)
    {
        Id = Guid.NewGuid();
        Created = DateTime.Now.ToUniversalTime();
        Processed = false;
        Content = content;
        Type = content.GetType().Name;
        CorrelationId = correlationId;
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
