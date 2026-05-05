using Microsoft.Extensions.Logging;
using OdectyStat1.Contracts;
using RabbitMQ.Client.Events;

namespace OdectyStat1.DataLayer.Consumers;

public class BinaryMessageConsumer : RabbitMQConsumer
{
    private readonly IBinaryMessageHandler handler;
    private readonly ILogger<BinaryMessageConsumer> logger;

    public BinaryMessageConsumer(
        RabbitMQProvider rabbitMQProvider,
        ILogger<BinaryMessageConsumer> logger,
        IBinaryMessageHandler handler)
        : base(rabbitMQProvider, logger, handler.QueueName)
    {
        this.handler = handler;
        this.logger = logger;
    }

    protected override async void ConsumerReceived(object? sender, BasicDeliverEventArgs e)
    {
        try
        {
            await handler.HandleAsync(e.Body, CancellationToken.None);
            AcknowledgeMessage(e.DeliveryTag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process binary message from queue {QueueName}", handler.QueueName);
            RejectMessage(e.DeliveryTag, requeue: false);
        }
    }
}
