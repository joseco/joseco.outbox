using Joseco.Outbox.Contracts.Model;

namespace Joseco.Outbox.EFCore.Persistence;

public interface IOutboxRepository<TContent>
{
    Task Update(OutboxMessage<TContent> message);

    Task<IEnumerable<OutboxMessage<TContent>>> GetUnprocessedAsync(int pageSize = 20);

}
