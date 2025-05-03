using Microsoft.EntityFrameworkCore;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Persistence;

namespace Joseco.Outbox.EFCore.Service;

internal class OutboxService<E>(IOutboxDatabase<E> outboxDatabase) : IOutboxService<E>, IOutboxRepository<E>
{
    public async Task AddAsync(OutboxMessage<E> message)
    {
        await outboxDatabase.GetOutboxMessages().AddAsync(message);
    }
    public Task Update(OutboxMessage<E> message)
    {
        outboxDatabase.GetOutboxMessages().Update(message);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<OutboxMessage<E>>> GetUnprocessedAsync(int pageSize = 20)
    {
        return await outboxDatabase.GetOutboxMessages().AsNoTracking().Where(x => !x.Processed).Take(pageSize).ToListAsync();
    }

}
