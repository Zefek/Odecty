using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdectyStat1.Application;
using OdectyStat1.DataLayer;
using OdectyStat1.Dto;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Globalization;
using System.Text;

namespace OdectyStat1.DataLayer.Consumers
{
    internal class RecognizedSuccess : BackgroundService
    {
        private IModel model;
        private EventingBasicConsumer consumer;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<RecognizedSuccess> logger;
        private bool inProcess = false;

        public RecognizedSuccess(IServiceProvider serviceProvider, RabbitMQProvider rabbitMQProvider, ILogger<RecognizedSuccess> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            model = rabbitMQProvider.CreateModel();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            try
            {
                consumer = new EventingBasicConsumer(model);
                consumer.Received += Consumer_Received;
                model.BasicConsume(QueuesToConsume.GaugeRecognizedSuccess, false, consumer);
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Keep the service running
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error create mq client: {message}", e.Message);
            }
        }

        private async void Consumer_Received(object? sender, BasicDeliverEventArgs e)
        {
            logger.LogInformation("Data received at: {time}", DateTimeOffset.Now);
            if (!inProcess)
            {
                try
                {
                    inProcess = true;
                    var body = Encoding.UTF8.GetString(e.Body.ToArray());
                    logger.LogInformation("Data received {body}", body);
                    dynamic message = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    using var scope = serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetService<IGaugeService>();
                    await service.GaugeRecognizedSucceeded(int.Parse(message.gaugeId.ToString()),message.file.ToString(), decimal.Parse(message.state.ToString(), CultureInfo.InvariantCulture));
                    model.BasicAck(e.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing data: {message}", ex.Message);
                }
                finally
                {
                    inProcess = false;
                }
            }
            else
            {
                //redeliver message
                model.BasicReject(e.DeliveryTag, true);
            }
        }

        public override void Dispose()
        {
            model?.Dispose();
            base.Dispose();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);
            model?.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}