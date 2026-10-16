using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using OdectyStat1.DataLayer;

namespace OdectyStat1.Reliability;

public class ReliabilityPublisher
{
    readonly MqttPublisher mqtt;
    readonly ReliabilitySettings settings;
    readonly ILogger<ReliabilityPublisher> logger;
    readonly JsonSerializerOptions jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public ReliabilityPublisher(MqttPublisher mqtt, ReliabilitySettings settings, ILogger<ReliabilityPublisher> logger)
    {
        this.mqtt = mqtt;
        this.settings = settings;
        this.logger = logger;
    }

    string BaseTopic => $"{settings.Options.HomeAssistant.BaseTopic}/{settings.Options.HomeAssistant.ObjectId}";

    public async Task Publish(ReliabilityReportBuilder builder, CancellationToken cancellationToken)
    {
        var ha = settings.Options.HomeAssistant;
        var discovery = new JsonObject
        {
            ["name"] = ha.Name,
            ["unique_id"] = ha.ObjectId,
            ["state_topic"] = $"{BaseTopic}/state",
            ["json_attributes_topic"] = $"{BaseTopic}/attributes",
            ["icon"] = "mdi:server-network",
            ["device"] = new JsonObject
            {
                ["identifiers"] = new JsonArray("reliability_report"),
                ["name"] = "Spolehlivost",
                ["manufacturer"] = "OdectyStat",
            },
        };
        var attributes = new JsonObject
        {
            ["day"] = builder.Facts?["day"]?.DeepClone(),
            ["generated_at"] = builder.Facts?["generated_at"]?.DeepClone(),
            ["comment"] = builder.Comment?.Text,
            ["comment_model"] = builder.Comment?.Model,
            ["highlights"] = new JsonArray(builder.Highlights
                .Select(h => (JsonNode)new JsonObject { ["level"] = h.Level, ["source"] = h.Source, ["text"] = h.Text })
                .ToArray()),
        };
        await mqtt.PublishRetained(
        [
            ($"homeassistant/sensor/{ha.ObjectId}/config", discovery.ToJsonString(jsonOptions)),
            ($"{BaseTopic}/state", builder.Status),
            ($"{BaseTopic}/attributes", attributes.ToJsonString(jsonOptions)),
        ], cancellationToken);
        logger.LogInformation("Reliability report {Day} published to Home Assistant as {ObjectId}: {Status}", builder.Facts?["day"], ha.ObjectId, builder.Status);
    }
}
