using Microsoft.Extensions.Options;
using OdectyStat1.Dto;

namespace OdectyStat1.DataLayer;

public class FirmwareFolderResolver
{
    private readonly IOptions<FirmwareLocation> location;

    public FirmwareFolderResolver(IOptions<FirmwareLocation> location)
    {
        this.location = location;
    }

    public string? Resolve(string deviceName)
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
