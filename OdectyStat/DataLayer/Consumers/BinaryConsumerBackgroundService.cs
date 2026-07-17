using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class BinaryConsumerBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<BinaryConsumerBackgroundService> logger;
    private readonly List<BinaryMessageConsumer> consumers = new();

    public BinaryConsumerBackgroundService(IServiceProvider serviceProvider, ILogger<BinaryConsumerBackgroundService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var handlers = serviceProvider.GetServices<IBinaryMessageHandler>();
        var provider = serviceProvider.GetRequiredService<RabbitMQProvider>();
        var consumerLogger = serviceProvider.GetRequiredService<ILogger<BinaryMessageConsumer>>();

        foreach (var handler in handlers)
        {
            consumers.Add(new BinaryMessageConsumer(provider, consumerLogger, handler));
            logger.LogInformation("Registered binary message handler for queue: {QueueName}", handler.QueueName);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var consumer in consumers)
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
                    logger.LogWarning(ex, "Failed to (re)start binary consumer, will retry.");
                }
            }

            await Task.Delay(1000, stoppingToken);
        }

        foreach (var consumer in consumers)
        {
            await consumer.StopConsuming();
            await consumer.DisposeAsync();
        }
    }
}
