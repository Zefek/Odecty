using System.Collections.Concurrent;

namespace OdectyStat1.DataLayer;

public class PendingGarageRequest
{
    public uint CorrelationId { get; init; }
    public required string Identity { get; init; }
    public DateTime RequestedAt { get; init; }
}

public class GarageHandshakeStore
{
    private readonly ConcurrentDictionary<uint, PendingGarageRequest> pending = new();

    public void Add(PendingGarageRequest request) => pending[request.CorrelationId] = request;

    public bool TryGet(uint correlationId, out PendingGarageRequest request)
        => pending.TryGetValue(correlationId, out request!);

    public void Remove(uint correlationId) => pending.TryRemove(correlationId, out _);

    public IReadOnlyCollection<PendingGarageRequest> Snapshot() => pending.Values.ToArray();
}
