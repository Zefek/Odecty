using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.DataLayer.Consumers;
public class ConsumerBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    public ConsumerBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var consumer in serviceProvider.GetServices<IRabbitMQConsumer>())
            {
                if (!consumer.IsConsuming)
                {
                    consumer.StartConsuming();
                }
            }
            // Placeholder for background service logic
            await Task.Delay(1000, stoppingToken);
        }
    }
}
