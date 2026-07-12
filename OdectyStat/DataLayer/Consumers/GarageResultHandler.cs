using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Application;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class GarageResultHandler : IBinaryMessageHandler
{
    public string QueueName => QueuesToConsume.GarageOpenResult;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<GarageResultHandler> logger;

    public GarageResultHandler(IServiceScopeFactory scopeFactory, ILogger<GarageResultHandler> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length < 5)
        {
            logger.LogWarning("Garage result too short: {Length} bytes", payload.Length);
            return;
        }
        var correlationId = BinaryPrimitives.ReadUInt32LittleEndian(payload.Span);
        var status = payload.Span[4];

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGarageCommandService>();
        await service.HandleResult(correlationId, status, ct);
    }
}
