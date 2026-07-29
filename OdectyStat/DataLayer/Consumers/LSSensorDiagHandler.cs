using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer.Consumers;

public class LSSensorDiagHandler : IBinaryMessageHandler
{
    private const int BaseSize = 18;
    private const int ExtendedSize = 24;

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
        if (payload.Length != BaseSize && payload.Length != ExtendedSize)
        {
            logger.LogError("LSSensor diag message has wrong size: {Length} bytes, expected {Base} or {Extended}", payload.Length, BaseSize, ExtendedSize);
            throw new InvalidDataException($"LSSensor diag payload size {payload.Length} != {BaseSize}/{ExtendedSize}");
        }

        var data = ParseDiagData(payload.Span);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
        db.LSSensorDiagnostics.Add(data);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Saved LSSensor diagnostic: uptime={Uptime}min, freeRam={FreeRam}kB, loopMax={LoopMax}ms, rssi={Rssi}dBm, fw={FwVersion}, otaFail={OtaFailCount}, samplerMax={SamplerMax}us, stackHwm={StackHwm}w, overruns={Overruns}",
            data.UptimeMinutes, data.FreeRam, data.LoopMaxMs, data.Rssi, data.FwVersion, data.OtaFailCount, data.SamplerMaxUs, data.SamplerStackWords, data.SamplerOverruns);
    }

    private static LSSensorDiagnostic ParseDiagData(ReadOnlySpan<byte> span)
    {
        // DiagData struct layout (little-endian, packed):
        // offset 0:  uint32 uptime (minutes)
        // offset 4:  uint16 freeRam (kilobytes)
        // offset 6:  uint16 wifiReconn
        // offset 8:  uint16 mqttFailCount
        // offset 10: uint8  resetReason
        // offset 11: uint16 loopMaxMs
        // offset 13: int8   rssi (dBm, signed)
        // offset 14: uint16 fwVersion
        // offset 16: uint16 otaFailCount
        // -- extended (24-byte payload only) --
        // offset 18: uint16 samplerMaxUs
        // offset 20: uint16 samplerStackWords
        // offset 22: uint16 samplerOverruns
        var diag = new LSSensorDiagnostic
        {
            Timestamp = DateTime.UtcNow,
            UptimeMinutes = BinaryPrimitives.ReadUInt32LittleEndian(span),
            FreeRam = BinaryPrimitives.ReadUInt16LittleEndian(span[4..]),
            WifiReconnects = BinaryPrimitives.ReadUInt16LittleEndian(span[6..]),
            MqttFailCount = BinaryPrimitives.ReadUInt16LittleEndian(span[8..]),
            ResetReason = span[10],
            LoopMaxMs = BinaryPrimitives.ReadUInt16LittleEndian(span[11..]),
            Rssi = (sbyte)span[13],
            FwVersion = BinaryPrimitives.ReadUInt16LittleEndian(span[14..]),
            OtaFailCount = BinaryPrimitives.ReadUInt16LittleEndian(span[16..])
        };

        if (span.Length >= ExtendedSize)
        {
            diag.SamplerMaxUs = BinaryPrimitives.ReadUInt16LittleEndian(span[18..]);
            diag.SamplerStackWords = BinaryPrimitives.ReadUInt16LittleEndian(span[20..]);
            diag.SamplerOverruns = BinaryPrimitives.ReadUInt16LittleEndian(span[22..]);
        }

        return diag;
    }
}
