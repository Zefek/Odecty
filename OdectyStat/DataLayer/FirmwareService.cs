using OdectyStat1.Contracts;
using OdectyStat1.Dto;

namespace OdectyStat1.DataLayer;

internal class FirmwareService : IFirmwareService
{
    private readonly FirmwareFolderResolver folderResolver;
    private readonly IFirmwareManifestReader manifestReader;

    public FirmwareService(FirmwareFolderResolver folderResolver, IFirmwareManifestReader manifestReader)
    {
        this.folderResolver = folderResolver;
        this.manifestReader = manifestReader;
    }

    public async Task<string?> GetManifestAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        return await manifestReader.ReadRawAsync(deviceName, cancellationToken);
    }

    public async Task<FirmwareFile?> GetFirmwareAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        var deviceFolder = folderResolver.Resolve(deviceName);
        if (deviceFolder is null)
        {
            return null;
        }

        var fileName = await manifestReader.ReadValueAsync(deviceName, "file", cancellationToken);
        if (fileName is null)
        {
            return null;
        }

        var firmwarePath = ResolveFirmwarePath(deviceFolder, fileName);
        if (firmwarePath is null)
        {
            return null;
        }

        return new FirmwareFile
        {
            Content = new FileStream(firmwarePath, FileMode.Open, FileAccess.Read, FileShare.Read),
            ContentType = "application/octet-stream",
            FileName = Path.GetFileName(firmwarePath)
        };
    }

    private static string? ResolveFirmwarePath(string deviceFolder, string fileName)
    {
        if (Path.IsPathRooted(fileName))
        {
            return null;
        }

        var normalized = fileName.Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(deviceFolder);
        var firmwarePath = Path.GetFullPath(Path.Combine(root, normalized));

        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!firmwarePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return File.Exists(firmwarePath) ? firmwarePath : null;
    }
}
