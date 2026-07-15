using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OdectyStat1.DataLayer.Consumers;
public class ConsumerBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ConsumerBackgroundService> logger;

    public ConsumerBackgroundService(IServiceProvider serviceProvider, ILogger<ConsumerBackgroundService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var consumer in serviceProvider.GetServices<IRabbitMQConsumer>())
            {
                try
                {
                    if (!consumer.IsConsuming)
                    {
                        await consumer.StartConsuming();
                    }
                    if (stoppingToken.IsCancellationRequested)
                    {
                        await consumer.StopConsuming();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to (re)start consumer, will retry.");
                }
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
