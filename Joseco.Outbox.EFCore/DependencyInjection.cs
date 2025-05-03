using Joseco.Outbox.EFCore.Procesor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Config;
using Joseco.Outbox.EFCore.Persistence;
using Joseco.Outbox.EFCore.Service;
using System.Reflection;

namespace Joseco.Outbox.EFCore;

public static class DependencyInjection
{
    public static ModelBuilder AddOutboxModel<E>(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfig<E>());

        return modelBuilder;
    }

    public static IServiceCollection AddOutbox<E>(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddScoped<IOutboxService<E>, OutboxService<E>>();
        services.AddScoped<IOutboxRepository<E>, OutboxService<E>>();
        services.AddScoped<OutboxProcessor<E>>();
        return services;
    }
}
