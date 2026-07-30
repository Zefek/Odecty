using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class GarageDiagHandler : IBinaryMessageHandler
{
    private const int BaseSize = 17;
    private const int ExtendedSize = 21;

    public string QueueName => QueuesToConsume.GarageDiag;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<GarageDiagHandler> logger;

    public GarageDiagHandler(IServiceScopeFactory scopeFactory, ILogger<GarageDiagHandler> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length != BaseSize && payload.Length != ExtendedSize)
        {
            logger.LogError("Garage diag message has wrong size: {Length} bytes, expected {Base} or {Extended}", payload.Length, BaseSize, ExtendedSize);
            throw new InvalidDataException($"Garage diag payload size {payload.Length} != {BaseSize}/{ExtendedSize}");
        }

        var data = ParseDiagData(payload.Span);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
        db.GarageDiagnostics.Add(data);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Saved Garage diagnostic: uptime={Uptime}min, freeRam={FreeRam}, loopMax={LoopMax}ms, doorCycles={DoorCycles}, rssi={Rssi}dBm, fw={FwVersion}, otaFail={OtaFailCount}",
            data.UptimeMinutes, data.FreeRam, data.LoopMaxMs, data.DoorCycles, data.Rssi, data.FwVersion, data.OtaFailCount);
    }

    private static GarageDiagnostic ParseDiagData(ReadOnlySpan<byte> span)
    {
        // DiagData struct layout (little-endian, packed):
        // offset 0:  uint32 uptime (minutes)
        // offset 4:  uint16 freeRam (bytes on AVR, kilobytes on ESP32)
        // offset 6:  uint16 wifiReconn
        // offset 8:  uint16 mqttReconn
        // offset 10: uint8  sensorErr (bitmask)
        // offset 11: uint8  resetReason (MCUSR on AVR, esp_reset_reason_t on ESP32)
        // offset 12: uint16 loopMaxMs
        // offset 14: uint16 doorCycles
        // offset 16: int8   rssi (dBm, signed)
        // -- extended (21-byte payload only) --
        // offset 17: uint16 fwVersion
        // offset 19: uint16 otaFailCount
        var diag = new GarageDiagnostic
        {
            Timestamp = DateTime.UtcNow,
            UptimeMinutes = BinaryPrimitives.ReadUInt32LittleEndian(span),
            FreeRam = BinaryPrimitives.ReadUInt16LittleEndian(span[4..]),
            WifiReconnects = BinaryPrimitives.ReadUInt16LittleEndian(span[6..]),
            MqttReconnects = BinaryPrimitives.ReadUInt16LittleEndian(span[8..]),
            SensorErrors = span[10],
            ResetReason = span[11],
            LoopMaxMs = BinaryPrimitives.ReadUInt16LittleEndian(span[12..]),
            DoorCycles = BinaryPrimitives.ReadUInt16LittleEndian(span[14..]),
            Rssi = (sbyte)span[16]
        };

        if (span.Length >= ExtendedSize)
        {
            diag.FwVersion = BinaryPrimitives.ReadUInt16LittleEndian(span[17..]);
            diag.OtaFailCount = BinaryPrimitives.ReadUInt16LittleEndian(span[19..]);
        }

        return diag;
    }
}
