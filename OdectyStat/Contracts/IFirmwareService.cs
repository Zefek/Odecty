using OdectyStat1.Dto;

namespace OdectyStat1.Contracts;

public interface IFirmwareService
{
    Task<string?> GetManifestAsync(string deviceName, CancellationToken cancellationToken = default);
    Task<FirmwareFile?> GetFirmwareAsync(string deviceName, CancellationToken cancellationToken = default);
}
