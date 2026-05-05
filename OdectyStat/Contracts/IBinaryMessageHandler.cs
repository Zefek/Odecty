namespace OdectyStat1.Contracts;

public interface IBinaryMessageHandler
{
    string QueueName { get; }
    Task HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken ct);
}
