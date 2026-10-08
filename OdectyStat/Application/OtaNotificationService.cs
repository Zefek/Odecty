using OdectyStat1.Contracts;

namespace OdectyStat1.Application;

public class OtaNotificationService : IOtaNotificationService
{
    private const string RoutingKeySuffix = ".OTA";
    private const string Payload = "OtaAvailable";

    private readonly IFirmwareManifestReader manifestReader;
    private readonly IMessageQueue messageQueue;
    private readonly ILogger<OtaNotificationService> logger;

    public OtaNotificationService(
        IFirmwareManifestReader manifestReader,
        IMessageQueue messageQueue,
        ILogger<OtaNotificationService> logger)
    {
        this.manifestReader = manifestReader;
        this.messageQueue = messageQueue;
        this.logger = logger;
    }

    public async Task NotifyIfOutdatedAsync(string deviceName, int? fwVersion, CancellationToken cancellationToken = default)
    {
        if (fwVersion == null)
        {
            return;
        }

        try
        {
            var manifestVersion = await manifestReader.ReadVersionAsync(deviceName, cancellationToken);
            if (manifestVersion == null)
            {
                logger.LogWarning("Manifest version not found for device {DeviceName}", deviceName);
                return;
            }

            if (fwVersion >= manifestVersion)
            {
                return;
            }

            await messageQueue.MQTTPublish(Payload, deviceName + RoutingKeySuffix);
            logger.LogInformation("OTA available for {DeviceName}: running fw={FwVersion}, manifest={ManifestVersion}",
                deviceName, fwVersion, manifestVersion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OTA notification failed for device {DeviceName}", deviceName);
        }
    }
}
