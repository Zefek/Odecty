using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OdectyStat1.DataLayer.Consumers;
public abstract class RabbitMQConsumer : IAsyncDisposable, IRabbitMQConsumer
{
    private readonly RabbitMQProvider rabbitMQProvider;
    private readonly ILogger<RabbitMQConsumer> logger;
    private readonly string queueName;
    private IChannel? model;

    public bool IsConsuming => model != null && !model.IsClosed;

    public RabbitMQConsumer(RabbitMQProvider rabbitMQProvider, ILogger<RabbitMQConsumer> logger, string queueName)
    {
        this.rabbitMQProvider = rabbitMQProvider;
        this.rabbitMQProvider.ConnectionShutdown += async (s, e) =>
        {
            await StopConsuming();
            logger.LogWarning("RabbitMQ connection shutdown detected. Stopped consuming.");
        };
        this.logger = logger;
        this.queueName = queueName;
    }

    public async Task StartConsuming()
    {
        if (model == null || model.IsClosed)
        {
            model = await rabbitMQProvider.CreateModel();
            if (model != null)
            {
                model.ChannelShutdownAsync += async (s, e) =>
                {
                    await StopConsuming();
                    logger.LogWarning("RabbitMQ model shutdown detected. Stopped consuming from queue: {QueueName}", queueName);
                };
            }
        }
        if (model == null)
        {
            logger.LogWarning("Failed to create RabbitMQ model for consuming.");
            await StopConsuming();
            return;
        }
        var consumer = new AsyncEventingBasicConsumer(model);
        consumer.ReceivedAsync += ConsumerReceived;
        await model.BasicConsumeAsync(queueName, false, consumer);
        logger.LogInformation("Started consuming from queue: {QueueName}", queueName);
    }

    protected abstract Task ConsumerReceived(object? sender, BasicDeliverEventArgs e);

    protected async Task AcknowledgeMessage(ulong deliveryTag)
    {
        if (model != null)
        {
            await model.BasicAckAsync(deliveryTag, false);
        }
    }

    protected async Task RejectMessage(ulong deliveryTag, bool requeue = false)
    {
        if (model != null)
        {
            await model.BasicRejectAsync(deliveryTag, requeue);
        }
    }

    public async Task StopConsuming()
    {
        if (model != null)
        {
            if (!model.IsClosed)
            {
                await model.CloseAsync();
            }
            model.Dispose();
        }
        model = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (model != null)
        {
            if (!model.IsClosed)
            {
                await model.CloseAsync();
            }
            await model.DisposeAsync();
        }
        model = null;
    }
}
