using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer;

public class FirmwareManifestReader : IFirmwareManifestReader
{
    private const string ManifestFileName = "manifest.txt";

    private readonly FirmwareFolderResolver folderResolver;

    public FirmwareManifestReader(FirmwareFolderResolver folderResolver)
    {
        this.folderResolver = folderResolver;
    }

    public async Task<string?> ReadRawAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        var manifestPath = ResolveManifestPath(deviceName);
        if (manifestPath is null)
        {
            return null;
        }

        return await File.ReadAllTextAsync(manifestPath, cancellationToken);
    }

    public async Task<string?> ReadValueAsync(string deviceName, string key, CancellationToken cancellationToken = default)
    {
        var manifestPath = ResolveManifestPath(deviceName);
        if (manifestPath is null)
        {
            return null;
        }

        foreach (var line in await File.ReadAllLinesAsync(manifestPath, cancellationToken))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            if (line[..separator].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                var value = line[(separator + 1)..].Trim();
                return value.Length > 0 ? value : null;
            }
        }

        return null;
    }

    public async Task<int?> ReadVersionAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        var value = await ReadValueAsync(deviceName, "version", cancellationToken);
        return int.TryParse(value, out var version) ? version : null;
    }

    private string? ResolveManifestPath(string deviceName)
    {
        var deviceFolder = folderResolver.Resolve(deviceName);
        if (deviceFolder is null)
        {
            return null;
        }

        var manifestPath = Path.Combine(deviceFolder, ManifestFileName);
        return File.Exists(manifestPath) ? manifestPath : null;
    }
}
