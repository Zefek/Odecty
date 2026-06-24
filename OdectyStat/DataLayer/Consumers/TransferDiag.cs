using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OdectyStat1.Business;
using OdectyStat1.Contracts;
using RabbitMQ.Client.Events;

namespace OdectyStat1.DataLayer.Consumers;

internal class TransferDiag : RabbitMQConsumer
{
    private const int ExpectedSize = 26;

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<TransferDiag> logger;
    private bool inProcess = false;

    public TransferDiag(IServiceProvider serviceProvider, RabbitMQProvider rabbitMQProvider, ILogger<TransferDiag> logger)
        : base(rabbitMQProvider, logger, QueuesToConsume.GaugeTransferDiag)
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
            logger.LogInformation("Transfer diag received {body}", body);
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
                logger.LogError("Transfer diag blob has wrong size: {Length} B, expected {Expected}", blob.Length, ExpectedSize);
                await RejectMessage(e.DeliveryTag);
                return;
            }

            var diag = Parse(blob, gaugeId, timestamp);

            using var scope = serviceProvider.CreateScope();
            var recorder = scope.ServiceProvider.GetRequiredService<IDiagnosticsRecorder>();
            await recorder.RecordTransferAsync(diag);

            await AcknowledgeMessage(e.DeliveryTag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing transfer diag: {message}", ex.Message);
            await RejectMessage(e.DeliveryTag);
        }
        finally
        {
            inProcess = false;
        }
    }

    private static TransferDiagnostic Parse(ReadOnlySpan<byte> s, int gaugeId, DateTime timestamp)
    {
        return new TransferDiagnostic
        {
            SchemaVer = s[0],
            CorrelationId = BinaryPrimitives.ReadUInt64LittleEndian(s[1..]),
            ImgSize = BinaryPrimitives.ReadUInt32LittleEndian(s[9..]),
            BytesSent = BinaryPrimitives.ReadUInt32LittleEndian(s[13..]),
            DurationMs = BinaryPrimitives.ReadUInt32LittleEndian(s[17..]),
            TryCount = s[21],
            Success = s[22] != 0,
            HttpCode = BinaryPrimitives.ReadInt16LittleEndian(s[23..]),
            Rssi = (sbyte)s[25],
            GaugeId = gaugeId,
            Timestamp = timestamp
        };
    }
}
