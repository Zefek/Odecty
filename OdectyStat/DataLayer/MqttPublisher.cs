using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using OdectyStat1.Dto;

namespace OdectyStat1.DataLayer;

public class MqttPublisher
{
    readonly IOptions<MqttSettings> options;
    readonly ILogger<MqttPublisher> logger;

    public MqttPublisher(IOptions<MqttSettings> options, ILogger<MqttPublisher> logger)
    {
        this.options = options;
        this.logger = logger;
    }

    public async Task PublishRetained(IEnumerable<(string Topic, string Payload)> messages, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new MqttClientFactory().CreateMqttClient();
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(settings.Host, settings.Port)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithClientId(settings.ClientId)
            .WithCleanSession(true)
            .WithCredentials(settings.Username, settings.Password);
        if (settings.UseTls)
        {
            builder.WithTlsOptions(new MqttClientTlsOptions { UseTls = true, CertificateValidationHandler = ValidateCertificate });
        }
        await client.ConnectAsync(builder.Build(), cancellationToken);
        try
        {
            foreach (var (topic, payload) in messages)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithRetainFlag()
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();
                await client.PublishAsync(message, cancellationToken);
            }
        }
        finally
        {
            await client.DisconnectAsync(cancellationToken: CancellationToken.None);
        }
    }

    bool ValidateCertificate(MqttClientCertificateValidationEventArgs context)
    {
        if (context.SslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }
        if (context.SslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors
            && context.Chain != null
            && context.Chain.ChainStatus.All(s => s.Status is X509ChainStatusFlags.NoError or X509ChainStatusFlags.RevocationStatusUnknown or X509ChainStatusFlags.OfflineRevocation))
        {
            return true;
        }
        logger.LogWarning("MQTT TLS certificate validation errors: {Errors} for {Subject}", context.SslPolicyErrors, context.Certificate?.Subject);
        return false;
    }
}
