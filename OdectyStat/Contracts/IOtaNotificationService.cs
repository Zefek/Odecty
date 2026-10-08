namespace OdectyStat1.Contracts;

public interface IOtaNotificationService
{
    Task NotifyIfOutdatedAsync(string deviceName, int? fwVersion, CancellationToken cancellationToken = default);
}
