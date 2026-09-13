using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class FveDiagHandler : IBinaryMessageHandler
{
    private const int ExpectedSize = 32;

    public string QueueName => QueuesToConsume.FveDiag;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<FveDiagHandler> logger;

    public FveDiagHandler(IServiceScopeFactory scopeFactory, ILogger<FveDiagHandler> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length < ExpectedSize)
        {
            logger.LogWarning("FVE diag message too short: {Length} bytes, expected {Expected}", payload.Length, ExpectedSize);
            return;
        }

        var data = ParseDiagData(payload.Span);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
        db.FveDiagnostics.Add(data);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Saved FVE diagnostic: uptime={Uptime}min, freeHeap={FreeHeap}kB, loopMax={LoopMax}ms, rssi={Rssi}dBm, fw={FwVersion}, belFrameErrors={BelFrameErrors}, dropouts={Dropouts}, raw={RawA}/{RawB}, ripple={RippleA}/{RippleB}",
            data.UptimeMinutes, data.FreeHeapKb, data.LoopMaxMs, data.Rssi, data.FwVersion, data.BelFrameErrors, data.LoadDropouts, data.RawA, data.RawB, data.RippleA, data.RippleB);
    }

    private static FveDiagnostic ParseDiagData(ReadOnlySpan<byte> span)
    {
        // DiagData struct layout (little-endian, packed):
        // offset 0:  uint32 uptimeMinutes
        // offset 4:  uint16 freeHeapKb
        // offset 6:  uint16 minFreeHeapKb
        // offset 8:  uint16 wifiReconnects
        // offset 10: uint16 mqttFailCount
        // offset 12: uint16 otaFailCount
        // offset 14: uint16 loopMaxMs
        // offset 16: uint16 belFrameErrors
        // offset 18: uint16 loadDropouts
        // offset 20: uint16 rawA
        // offset 22: uint16 rawB
        // offset 24: uint16 rippleA
        // offset 26: uint16 rippleB
        // offset 28: uint8  resetReason
        // offset 29: uint16 fwVersion
        // offset 31: int8   rssi (dBm, signed)
        return new FveDiagnostic
        {
            Timestamp = DateTime.UtcNow,
            UptimeMinutes = BinaryPrimitives.ReadUInt32LittleEndian(span),
            FreeHeapKb = BinaryPrimitives.ReadUInt16LittleEndian(span[4..]),
            MinFreeHeapKb = BinaryPrimitives.ReadUInt16LittleEndian(span[6..]),
            WifiReconnects = BinaryPrimitives.ReadUInt16LittleEndian(span[8..]),
            MqttFailCount = BinaryPrimitives.ReadUInt16LittleEndian(span[10..]),
            OtaFailCount = BinaryPrimitives.ReadUInt16LittleEndian(span[12..]),
            LoopMaxMs = BinaryPrimitives.ReadUInt16LittleEndian(span[14..]),
            BelFrameErrors = BinaryPrimitives.ReadUInt16LittleEndian(span[16..]),
            LoadDropouts = BinaryPrimitives.ReadUInt16LittleEndian(span[18..]),
            RawA = BinaryPrimitives.ReadUInt16LittleEndian(span[20..]),
            RawB = BinaryPrimitives.ReadUInt16LittleEndian(span[22..]),
            RippleA = BinaryPrimitives.ReadUInt16LittleEndian(span[24..]),
            RippleB = BinaryPrimitives.ReadUInt16LittleEndian(span[26..]),
            ResetReason = span[28],
            FwVersion = BinaryPrimitives.ReadUInt16LittleEndian(span[29..]),
            Rssi = (sbyte)span[31]
        };
    }
}
