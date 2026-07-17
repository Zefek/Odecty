namespace OdectyStat1.Application;

public interface IGarageCommandService
{
    Task<uint> RequestOpen(string identity, CancellationToken cancellationToken);
    Task HandleChallenge(uint correlationId, ReadOnlyMemory<byte> nonce, CancellationToken cancellationToken);
    Task HandleResult(uint correlationId, byte status, CancellationToken cancellationToken);
}
