using Joseco.Outbox.EFCore.Procesor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Config;
using Joseco.Outbox.EFCore.Persistence;
using Joseco.Outbox.EFCore.Service;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Joseco.Outbox.EFCore;

public static class DependencyInjection
{

    public static IServiceCollection AddOutbox<E>(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddScoped<IOutboxService<E>, OutboxService<E>>();
        services.AddScoped<IOutboxRepository<E>, OutboxService<E>>();
        services.AddScoped<OutboxProcessor<E>>();
        return services;
    }

    public static IServiceCollection AddOutboxBackgroundService<E>(this IServiceCollection services, int delay = 5000)
    {
        var serviceProvider = services.BuildServiceProvider();
        services.AddHostedService(c =>
        {
            OutboxBackgroundService<E> outboxBackgroundService = new(
                serviceProvider.GetRequiredService<ILogger<OutboxBackgroundService<E>>>(),
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                delay
            );
            return outboxBackgroundService;
        });
        return services;
    }
}
