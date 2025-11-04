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
        bool stopRequired = false;
        while (!stopRequired)
        {
            foreach (var consumer in serviceProvider.GetServices<IRabbitMQConsumer>())
            {
                if (!consumer.IsConsuming)
                {
                    consumer.StartConsuming();
                }
                if (stoppingToken.IsCancellationRequested)
                {
                    consumer.StopConsuming();
                }
            }
            // Placeholder for background service logic
            await Task.Delay(1000, stoppingToken);
            stopRequired = stoppingToken.IsCancellationRequested;
        }
    }
}
