using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class FveDiagHandler : IBinaryMessageHandler
{
    private const int BaseSize = 32;
    private const int ExtendedSize = 40;

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
        if (payload.Length < BaseSize)
        {
            logger.LogWarning("FVE diag message too short: {Length} bytes, expected {Expected}", payload.Length, BaseSize);
            return;
        }

        var data = ParseDiagData(payload.Span);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
        db.FveDiagnostics.Add(data);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Saved FVE diagnostic: uptime={Uptime}min, freeHeap={FreeHeap}kB, loopMax={LoopMax}ms, rssi={Rssi}dBm, fw={FwVersion}, belFrameErrors={BelFrameErrors}, dropouts={Dropouts}, raw={RawA}/{RawB}, ripple={RippleA}/{RippleB}, fanRpm={FanRpmA}/{FanRpmB}, fanRun={FanRunPctA}/{FanRunPctB}%, fanMismatch={FanMismatchSlots}",
            data.UptimeMinutes, data.FreeHeapKb, data.LoopMaxMs, data.Rssi, data.FwVersion, data.BelFrameErrors, data.LoadDropouts, data.RawA, data.RawB, data.RippleA, data.RippleB, data.FanRpmA, data.FanRpmB, data.FanRunPctA, data.FanRunPctB, data.FanMismatchSlots);
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
        // -- extended (40-byte payload only) --
        // offset 32: uint16 fanRpmA
        // offset 34: uint16 fanRpmB
        // offset 36: uint8  fanRunPctA
        // offset 37: uint8  fanRunPctB
        // offset 38: uint16 fanMismatchSlots
        var diag = new FveDiagnostic
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

        if (span.Length >= ExtendedSize)
        {
            diag.FanRpmA = BinaryPrimitives.ReadUInt16LittleEndian(span[32..]);
            diag.FanRpmB = BinaryPrimitives.ReadUInt16LittleEndian(span[34..]);
            diag.FanRunPctA = span[36];
            diag.FanRunPctB = span[37];
            diag.FanMismatchSlots = BinaryPrimitives.ReadUInt16LittleEndian(span[38..]);
        }

        return diag;
    }
}
