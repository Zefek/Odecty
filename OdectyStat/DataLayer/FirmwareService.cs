using Microsoft.Extensions.Options;
using OdectyStat1.Contracts;
using OdectyStat1.Dto;

namespace OdectyStat1.DataLayer;

internal class FirmwareService : IFirmwareService
{
    private const string ManifestFileName = "manifest.txt";

    private readonly IOptions<FirmwareLocation> location;

    public FirmwareService(IOptions<FirmwareLocation> location)
    {
        this.location = location;
    }

    public async Task<string?> GetManifestAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        var deviceFolder = ResolveDeviceFolder(deviceName);
        if (deviceFolder is null)
        {
            return null;
        }

        var manifestPath = Path.Combine(deviceFolder, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(manifestPath, cancellationToken);
    }

    public async Task<FirmwareFile?> GetFirmwareAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        var deviceFolder = ResolveDeviceFolder(deviceName);
        if (deviceFolder is null)
        {
            return null;
        }

        var manifestPath = Path.Combine(deviceFolder, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var fileName = await ReadManifestFileNameAsync(manifestPath, cancellationToken);
        if (fileName is null)
        {
            return null;
        }

        // Soubor z manifestu musí být prostý název v rámci složky zařízení (žádné cesty mimo ni).
        if (Path.GetFileName(fileName) != fileName)
        {
            return null;
        }

        var firmwarePath = Path.Combine(deviceFolder, fileName);
        if (!File.Exists(firmwarePath))
        {
            return null;
        }

        return new FirmwareFile
        {
            Content = new FileStream(firmwarePath, FileMode.Open, FileAccess.Read, FileShare.Read),
            ContentType = "application/octet-stream",
            FileName = fileName
        };
    }

    private static async Task<string?> ReadManifestFileNameAsync(string manifestPath, CancellationToken cancellationToken)
    {
        foreach (var line in await File.ReadAllLinesAsync(manifestPath, cancellationToken))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            if (key.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                var value = line[(separator + 1)..].Trim();
                return value.Length > 0 ? value : null;
            }
        }

        return null;
    }

    private string? ResolveDeviceFolder(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return null;
        }

        // Ochrana proti path traversal: povol jen prostý název adresáře.
        if (deviceName != Path.GetFileName(deviceName)
            || deviceName.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        var root = Path.GetFullPath(location.Value.Path);
        var deviceFolder = Path.GetFullPath(Path.Combine(root, deviceName));

        // Výsledná cesta musí zůstat uvnitř kořene.
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!deviceFolder.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Directory.Exists(deviceFolder) ? deviceFolder : null;
    }
}
