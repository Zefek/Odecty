using Microsoft.AspNetCore.Mvc;
using OdectyStat1.Contracts;

namespace OdectyStat1.Controllers;

[ApiController]
[Route("api/firmware")]
public class FirmwareController : ControllerBase
{
    private readonly IFirmwareService firmwareService;

    public FirmwareController(IFirmwareService firmwareService)
    {
        this.firmwareService = firmwareService;
    }

    [HttpGet("{deviceName}/manifest")]
    public async Task<IActionResult> GetManifest(string deviceName, CancellationToken cancellationToken)
    {
        var manifest = await firmwareService.GetManifestAsync(deviceName, cancellationToken);
        if (manifest is null)
        {
            return NotFound();
        }

        return Content(manifest, "text/plain");
    }

    [HttpGet("{deviceName}/firmware")]
    public async Task<IActionResult> GetFirmware(string deviceName, CancellationToken cancellationToken)
    {
        var firmware = await firmwareService.GetFirmwareAsync(deviceName, cancellationToken);
        if (firmware is null)
        {
            return NotFound();
        }

        return File(firmware.Content, firmware.ContentType, firmware.FileName);
    }
}
