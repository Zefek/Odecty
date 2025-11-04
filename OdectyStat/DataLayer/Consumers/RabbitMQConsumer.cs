using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OdectyStat1.DataLayer.Consumers;
public abstract class RabbitMQConsumer : IDisposable, IRabbitMQConsumer
{
    private readonly RabbitMQProvider rabbitMQProvider;
    private readonly ILogger<RabbitMQConsumer> logger;
    private readonly string queueName;
    private IModel model;

    public bool IsConsuming => model != null && !model.IsClosed;

    public RabbitMQConsumer(RabbitMQProvider rabbitMQProvider, ILogger<RabbitMQConsumer> logger, string queueName)
    {
        this.rabbitMQProvider = rabbitMQProvider;
        this.rabbitMQProvider.ConnectionShutdown += (s, e) =>
        {
            StopConsuming();
            logger.LogWarning("RabbitMQ connection shutdown detected. Stopped consuming.");
        };
        this.logger = logger;
        this.queueName = queueName;
    }

    public void StartConsuming()
    {
        if (model == null || model.IsClosed)
        {
            model = rabbitMQProvider.CreateModel();
            model.ModelShutdown += (s, e) =>
            {
                StopConsuming();
                logger.LogWarning("RabbitMQ model shutdown detected. Stopped consuming from queue: {QueueName}", queueName);
            };
        }
        if (model == null)
        {
            logger.LogWarning("Failed to create RabbitMQ model for consuming.");
            StopConsuming();
            return;
        }
        var consumer = new EventingBasicConsumer(model);
        consumer.Received += ConsumerReceived;
        model.BasicConsume(queueName, false, consumer);
        logger.LogInformation("Started consuming from queue: {QueueName}", queueName);
    }

    protected abstract void ConsumerReceived(object? sender, BasicDeliverEventArgs e);

    protected void AcknowledgeMessage(ulong deliveryTag)
    {
        model?.BasicAck(deliveryTag, false);
    }

    protected void RejectMessage(ulong deliveryTag, bool requeue = false)
    {
        model?.BasicReject(deliveryTag, requeue);
    }

    public void StopConsuming()
    {
        model?.Close();
        model?.Dispose();
        model = null;
    }

    public void Dispose()
    {
        model?.Close();
        model?.Dispose();
        model = null;
    }
}
