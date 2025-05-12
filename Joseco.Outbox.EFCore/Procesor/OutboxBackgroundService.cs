using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joseco.Outbox.EFCore.Procesor;

public class OutboxBackgroundService<T>(ILogger<OutboxBackgroundService<T>> logger, 
    IServiceScopeFactory scopeFactory,
    int delay = 5000) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            try
            {
                using var scope = scopeFactory.CreateScope();
                OutboxProcessor<T> processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<T>>();
                await processor.Process(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error: {message}", ex.Message);
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}