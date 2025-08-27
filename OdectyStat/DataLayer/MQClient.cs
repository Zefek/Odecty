using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdectyMVC.Application;
using OdectyMVC.Contracts;
using OdectyStat.Dto;
using OdectyStat1.Dto;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat
{
    internal class MQClient : BackgroundService
    {
        private IConnection connection;
        private IModel model;
        private EventingBasicConsumer consumer;
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<OdectySettings> options;
        private readonly ILogger<MQClient> logger;
        private bool inProcess = false;

        public MQClient(IServiceProvider serviceProvider, IOptions<OdectySettings> options, ILogger<MQClient> logger) 
        {
            this.serviceProvider=serviceProvider;
            this.options=options;
            this.logger=logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            try
            {
                var factory = new ConnectionFactory();
                factory.HostName=options.Value.RabbitMQHost;
                factory.UserName = options.Value.RabbitMQUsername;
                factory.Password = options.Value.RabbitMQPassword;
                factory.VirtualHost = options.Value.RabbitMQVHost;

                connection = factory.CreateConnection();
                model = connection.CreateModel();

                model.ExchangeDeclare(options.Value.RabbitMQQueue, ExchangeType.Direct, true, false, null);
                model.QueueDeclare(options.Value.RabbitMQQueue, true, false, false, null);
                foreach(var map in options.Value.QueueMappings)
                {
                    model.QueueBind(map.QueueName, map.ExchangeName, map.RoutingKey);
                }
                consumer = new EventingBasicConsumer(model);
                consumer.Received+=Consumer_Received;
                model.BasicConsume(options.Value.RabbitMQQueue, false, consumer);
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Keep the service running
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch(Exception e)
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
                    var newValue = Newtonsoft.Json.JsonConvert.DeserializeObject<NewValue>(body);
                    using var scope = serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetService<IGaugeService>();
                    await service.AddNewValue(newValue);
                    model.BasicAck(e.DeliveryTag, false);
                }
                catch(Exception ex)
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
            connection?.Dispose();
            base.Dispose();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);
            model?.Dispose();
            connection?.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}