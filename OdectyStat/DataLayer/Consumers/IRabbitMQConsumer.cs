namespace OdectyStat1.DataLayer.Consumers;

public interface IRabbitMQConsumer
{
    bool IsConsuming { get; }

    void Dispose();
    void StartConsuming();
    void StopConsuming();
}