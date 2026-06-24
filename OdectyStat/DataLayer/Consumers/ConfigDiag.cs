using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OdectyStat1.Business;
using OdectyStat1.Contracts;
using RabbitMQ.Client.Events;

namespace OdectyStat1.DataLayer.Consumers;

internal class ConfigDiag : RabbitMQConsumer
{
    private const int ExpectedSize = 28;

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ConfigDiag> logger;
    private bool inProcess = false;

    public ConfigDiag(IServiceProvider serviceProvider, RabbitMQProvider rabbitMQProvider, ILogger<ConfigDiag> logger)
        : base(rabbitMQProvider, logger, QueuesToConsume.GaugeConfig)
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
            logger.LogInformation("Camera config received {body}", body);
            dynamic? message = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
            if (message == null)
            {
                await RejectMessage(e.DeliveryTag);
                return;
            }

            int gaugeId = int.Parse(message.GaugeId.ToString());
            DateTime timestamp = DateTime.Parse(message.Datetime.ToString()).ToUniversalTime();
            byte[] blob = Convert.FromBase64String((string)message.Data.ToString());
            if (blob.Length != ExpectedSize)
            {
                logger.LogError("Camera config blob has wrong size: {Length} B, expected {Expected}", blob.Length, ExpectedSize);
                await RejectMessage(e.DeliveryTag);
                return;
            }

            var config = Parse(blob, gaugeId, timestamp);

            using var scope = serviceProvider.CreateScope();
            var recorder = scope.ServiceProvider.GetRequiredService<IDiagnosticsRecorder>();
            await recorder.RecordConfigAsync(config);

            await AcknowledgeMessage(e.DeliveryTag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing camera config: {message}", ex.Message);
            await RejectMessage(e.DeliveryTag);
        }
        finally
        {
            inProcess = false;
        }
    }

    private static CameraConfig Parse(ReadOnlySpan<byte> s, int gaugeId, DateTime timestamp)
    {
        return new CameraConfig
        {
            SchemaVer = s[0],
            FwVersion = BinaryPrimitives.ReadUInt16LittleEndian(s[1..]),
            Framesize = s[3],
            Quality = s[4],
            AecValue = BinaryPrimitives.ReadUInt16LittleEndian(s[5..]),
            ExposureCtrl = s[7],
            GainCtrl = s[8],
            AgcGain = s[9],
            AeLevel = (sbyte)s[10],
            Brightness = (sbyte)s[11],
            Contrast = (sbyte)s[12],
            Saturation = (sbyte)s[13],
            Whitebal = s[14],
            AwbGain = s[15],
            WbMode = s[16],
            SpecialEffect = s[17],
            Hmirror = s[18],
            Vflip = s[19],
            Aec2 = s[20],
            Gainceiling = s[21],
            Dcw = s[22],
            Bpc = s[23],
            Wpc = s[24],
            RawGma = s[25],
            Lenc = s[26],
            CfgHash = s[27],
            GaugeId = gaugeId,
            Timestamp = timestamp
        };
    }
}
