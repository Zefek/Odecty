namespace OdectyStat1.Contracts;

public interface IFirmwareManifestReader
{
    Task<string?> ReadRawAsync(string deviceName, CancellationToken cancellationToken = default);
    Task<string?> ReadValueAsync(string deviceName, string key, CancellationToken cancellationToken = default);
    Task<int?> ReadVersionAsync(string deviceName, CancellationToken cancellationToken = default);
}
