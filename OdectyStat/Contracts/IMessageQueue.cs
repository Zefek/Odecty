namespace OdectyStat1.Contracts
{
    public interface IMessageQueue
    {
        Task Publish(object message, string routingKey);
        Task MQTTPublish(string message, string routingKey);
    }
}
