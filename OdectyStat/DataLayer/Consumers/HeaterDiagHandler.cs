using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class HeaterDiagHandler : IBinaryMessageHandler
{
    private const int ExpectedSize = 16;

    public string QueueName => QueuesToConsume.HeaterDiag;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<HeaterDiagHandler> logger;

    public HeaterDiagHandler(IServiceScopeFactory scopeFactory, ILogger<HeaterDiagHandler> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length < ExpectedSize)
        {
            logger.LogWarning("Heater diag message too short: {Length} bytes, expected {Expected}", payload.Length, ExpectedSize);
            return;
        }

        var data = ParseDiagData(payload.Span);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
        db.HeaterDiagnostics.Add(data);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Saved heater diagnostic: uptime={Uptime}min, freeRam={FreeRam}B, loopMax={LoopMax}ms, rssi={Rssi}dBm",
            data.UptimeMinutes, data.FreeRam, data.LoopMaxMs, data.Rssi);
    }

    private static HeaterDiagnostic ParseDiagData(ReadOnlySpan<byte> span)
    {
        // DiagData struct layout (AVR little-endian, packed):
        // offset 0:  uint32 uptime (minutes)
        // offset 4:  uint16 freeRam (bytes)
        // offset 6:  uint16 wifiReconn
        // offset 8:  uint16 mqttReconn
        // offset 10: uint16 sensorErr (bitmask)
        // offset 12: uint8  resetReason (MCUSR)
        // offset 13: uint16 loopMaxMs
        // offset 15: int8   rssi (dBm, signed)
        return new HeaterDiagnostic
        {
            Timestamp = DateTime.UtcNow,
            UptimeMinutes = BinaryPrimitives.ReadUInt32LittleEndian(span),
            FreeRam = BinaryPrimitives.ReadUInt16LittleEndian(span[4..]),
            WifiReconnects = BinaryPrimitives.ReadUInt16LittleEndian(span[6..]),
            MqttReconnects = BinaryPrimitives.ReadUInt16LittleEndian(span[8..]),
            SensorErrors = BinaryPrimitives.ReadUInt16LittleEndian(span[10..]),
            ResetReason = span[12],
            LoopMaxMs = BinaryPrimitives.ReadUInt16LittleEndian(span[13..]),
            Rssi = (sbyte)span[15]
        };
    }
}
