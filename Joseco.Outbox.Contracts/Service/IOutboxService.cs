using Joseco.Outbox.Contracts.Model;

namespace Joseco.Outbox.Contracts.Service;

public interface IOutboxService<TContent>
{
    Task AddAsync(OutboxMessage<TContent> message);
    
}
