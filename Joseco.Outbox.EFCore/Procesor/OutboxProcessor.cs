using MediatR;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;

namespace Joseco.Outbox.EFCore.Procesor;

public class OutboxProcessor<E>(IOutboxRepository<E> outboxRepository, IOutboxDatabase<E> outboxDatabase, IPublisher publisher)
{
    public async Task Process(CancellationToken cancellationToken)
    {
        IEnumerable<OutboxMessage<E>> messages = await outboxRepository.GetUnprocessedAsync();

        foreach (var item in messages)
        {
            if(item == null || item.Content is null)
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
