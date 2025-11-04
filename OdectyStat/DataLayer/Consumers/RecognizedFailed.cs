using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdectyStat1.Application;
using OdectyStat1.DataLayer;
using OdectyStat1.Dto;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Policy;
using System.Text;

namespace OdectyStat1.DataLayer.Consumers
{
    internal class RecognizedFailed : RabbitMQConsumer
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<GaugeImageLocation> options;
        private readonly ILogger<RecognizedFailed> logger;
        private bool inProcess = false;

        public RecognizedFailed(IServiceProvider serviceProvider, IOptions<GaugeImageLocation> options, ILogger<RecognizedFailed> logger, RabbitMQProvider rabbitMQProvider) : base(rabbitMQProvider, logger, QueuesToConsume.GaugeRecognizedFailed)
        {
            this.serviceProvider = serviceProvider;
            this.options = options;
            this.logger = logger;
        }

        protected override void ConsumerReceived(object? sender, BasicDeliverEventArgs e)
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
                    service.GaugeRecognizedFailed(int.Parse(message.gaugeId.ToString()), message.file.ToString());
                    AcknowledgeMessage(e.DeliveryTag);
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
                RejectMessage(e.DeliveryTag, true);
            }
        }
    }
}