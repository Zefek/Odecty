using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class LSSensorDiagHandler : IBinaryMessageHandler
{
    private const int ExpectedSize = 13;

    public string QueueName => QueuesToConsume.LSSensorDiag;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<LSSensorDiagHandler> logger;

    public LSSensorDiagHandler(IServiceScopeFactory scopeFactory, ILogger<LSSensorDiagHandler> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length != ExpectedSize)
        {
            logger.LogError("LSSensor diag message has wrong size: {Length} bytes, expected {Expected}", payload.Length, ExpectedSize);
            throw new InvalidDataException($"LSSensor diag payload size {payload.Length} != {ExpectedSize}");
        }

        var data = ParseDiagData(payload.Span);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
        db.LSSensorDiagnostics.Add(data);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Saved LSSensor diagnostic: uptime={Uptime}min, freeRam={FreeRam}B, loopMax={LoopMax}ms",
            data.UptimeMinutes, data.FreeRam, data.LoopMaxMs);
    }

    private static LSSensorDiagnostic ParseDiagData(ReadOnlySpan<byte> span)
    {
        // DiagData struct layout (little-endian, packed):
        // offset 0:  uint32 uptime (minutes)
        // offset 4:  uint16 freeRam (bytes)
        // offset 6:  uint16 wifiReconn
        // offset 8:  uint16 mqttFailCount
        // offset 10: uint8  resetReason
        // offset 11: uint16 loopMaxMs
        return new LSSensorDiagnostic
        {
            Timestamp = DateTime.UtcNow,
            UptimeMinutes = BinaryPrimitives.ReadUInt32LittleEndian(span),
            FreeRam = BinaryPrimitives.ReadUInt16LittleEndian(span[4..]),
            WifiReconnects = BinaryPrimitives.ReadUInt16LittleEndian(span[6..]),
            MqttFailCount = BinaryPrimitives.ReadUInt16LittleEndian(span[8..]),
            ResetReason = span[10],
            LoopMaxMs = BinaryPrimitives.ReadUInt16LittleEndian(span[11..])
        };
    }
}
