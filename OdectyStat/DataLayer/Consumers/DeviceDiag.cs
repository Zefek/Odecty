using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;
using RabbitMQ.Client.Events;

namespace OdectyStat1.DataLayer.Consumers;

internal class DeviceDiag : RabbitMQConsumer
{
    private const int ExpectedSize = 29;

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<DeviceDiag> logger;
    private bool inProcess = false;

    public DeviceDiag(IServiceProvider serviceProvider, RabbitMQProvider rabbitMQProvider, ILogger<DeviceDiag> logger)
        : base(rabbitMQProvider, logger, QueuesToConsume.GaugeDeviceDiag)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected override async Task ConsumerReceived(object? sender, BasicDeliverEventArgs e)
    {
        if (inProcess)
        {
            await RejectMessage(e.DeliveryTag, true);
            return;
        }
        try
        {
            inProcess = true;
            var body = Encoding.UTF8.GetString(e.Body.ToArray());
            logger.LogInformation("Device diag received {body}", body);
            dynamic? message = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
            if (message == null)
            {
                await RejectMessage(e.DeliveryTag);
                return;
            }

            int gaugeId = int.Parse(message.GaugeId.ToString());
            DateTime timestamp = ((DateTimeOffset)message.Datetime).UtcDateTime;
            byte[] blob = Convert.FromBase64String((string)message.Data.ToString());
            if (blob.Length != ExpectedSize)
            {
                logger.LogError("Device diag blob has wrong size: {Length} B, expected {Expected}", blob.Length, ExpectedSize);
                await RejectMessage(e.DeliveryTag);
                return;
            }

            var diag = Parse(blob, gaugeId, timestamp);

            using var scope = serviceProvider.CreateScope();
            var recorder = scope.ServiceProvider.GetRequiredService<IDiagnosticsRecorder>();
            await recorder.RecordDeviceAsync(diag);

            await AcknowledgeMessage(e.DeliveryTag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing device diag: {message}", ex.Message);
            await RejectMessage(e.DeliveryTag);
        }
        finally
        {
            inProcess = false;
        }
    }

    private static DeviceDiagnostic Parse(ReadOnlySpan<byte> s, int gaugeId, DateTime timestamp)
    {
        return new DeviceDiagnostic
        {
            SchemaVer = s[0],
            UptimeSeconds = BinaryPrimitives.ReadUInt32LittleEndian(s[1..]),
            FwVersion = BinaryPrimitives.ReadUInt16LittleEndian(s[5..]),
            FreeHeapKb = BinaryPrimitives.ReadUInt16LittleEndian(s[7..]),
            MinFreeHeapKb = BinaryPrimitives.ReadUInt16LittleEndian(s[9..]),
            MaxAllocKb = BinaryPrimitives.ReadUInt16LittleEndian(s[11..]),
            WifiReconnects = BinaryPrimitives.ReadUInt16LittleEndian(s[13..]),
            CaptureCount = BinaryPrimitives.ReadUInt16LittleEndian(s[15..]),
            SendFailures = BinaryPrimitives.ReadUInt16LittleEndian(s[17..]),
            TlsErrors = BinaryPrimitives.ReadUInt16LittleEndian(s[19..]),
            OtaFailures = BinaryPrimitives.ReadUInt16LittleEndian(s[21..]),
            LoopMaxMs = BinaryPrimitives.ReadUInt16LittleEndian(s[23..]),
            CameraErrors = s[25],
            ResetReason = s[26],
            Rssi = (sbyte)s[27],
            CfgHash = s[28],
            GaugeId = gaugeId,
            Timestamp = timestamp
        };
    }
}
