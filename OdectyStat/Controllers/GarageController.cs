using System.Net;
using Microsoft.AspNetCore.Mvc;
using OdectyStat1.Application;

namespace OdectyStat1.Controllers;

[ApiController]
[Route("internal/garage")]
public class GarageController : ControllerBase
{
    private readonly IGarageCommandService service;

    public GarageController(IGarageCommandService service)
    {
        this.service = service;
    }

    [HttpPost("command")]
    public async Task<IActionResult> Command([FromBody] GarageCommandRequest request, CancellationToken cancellationToken)
    {
        var remote = HttpContext.Connection.RemoteIpAddress;
        if (remote == null || !IPAddress.IsLoopback(remote))
        {
            return Forbid();
        }
        if (string.IsNullOrWhiteSpace(request.Identity))
        {
            return BadRequest();
        }
        var r = await service.RequestOpen(request.Identity, cancellationToken);
        return Accepted(new { r });
    }
}

public record GarageCommandRequest(string Identity);
