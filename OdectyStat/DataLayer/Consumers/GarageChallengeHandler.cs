using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Application;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class GarageChallengeHandler : IBinaryMessageHandler
{
    public string QueueName => QueuesToConsume.GarageOpenChallenge;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<GarageChallengeHandler> logger;

    public GarageChallengeHandler(IServiceScopeFactory scopeFactory, ILogger<GarageChallengeHandler> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length < 5)
        {
            logger.LogWarning("Garage challenge too short: {Length} bytes", payload.Length);
            return;
        }
        var correlationId = BinaryPrimitives.ReadUInt32LittleEndian(payload.Span);
        var nonce = payload[4..];

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGarageCommandService>();
        await service.HandleChallenge(correlationId, nonce, ct);
    }
}
