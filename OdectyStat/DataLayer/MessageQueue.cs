using Microsoft.Extensions.Options;
using OdectyStat1.Contracts;
using OdectyStat1.Dto;
using RabbitMQ.Client;
using System.Text;

namespace OdectyStat1.DataLayer;
public class MessageQueue : IMessageQueue, IDisposable
{
    private IChannel? model;
    private readonly RabbitMQProvider rabbitMQProvider;
    private readonly IOptions<OdectySettings> options;
    private readonly ILogger<MessageQueue> logger;

    public MessageQueue(RabbitMQProvider rabbitMQProvider, IOptions<OdectySettings> options, ILogger<MessageQueue> logger)
    {
        this.rabbitMQProvider = rabbitMQProvider;
        this.options = options;
        this.logger = logger;
    }
    public async Task Publish(object message, string routingKey)
    {
        if(model == null)
        {
            model = await rabbitMQProvider.CreateModel();
            if(model == null)
            {
                logger.LogWarning("Channel could not be created.");
                return;
            }
        }
        await model.BasicPublishAsync(exchange: options.Value.ExchangeName,
                             routingKey: routingKey,
                             mandatory: false,
                             basicProperties: new BasicProperties(),
                             body: Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message)));
    }

    public async Task MQTTPublish(string message, string routingKey)
    {
        await MQTTPublish(Encoding.UTF8.GetBytes(message), routingKey);
    }

    public async Task MQTTPublish(byte[] message, string routingKey)
    {
        if (model == null)
        {
            model = await rabbitMQProvider.CreateModel();
            if (model == null)
            {
                logger.LogWarning("Channel could not be created.");
                return;
            }
        }
        await model.BasicPublishAsync(exchange: "amq.topic",
                             routingKey: routingKey,
                             mandatory: false,
                             basicProperties: new BasicProperties(),
                             body: message);
    }

    public void Dispose()
    {
        model?.Dispose();
    }
}
