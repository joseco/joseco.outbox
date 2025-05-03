using Microsoft.EntityFrameworkCore;
using Joseco.Outbox.Contracts.Model;

namespace Joseco.Outbox.EFCore.Persistence;

public interface IOutboxDatabase<E>
{
    DbSet<OutboxMessage<E>> GetOutboxMessages();
    Task CommitAsync(CancellationToken cancellationToken = default);
}
