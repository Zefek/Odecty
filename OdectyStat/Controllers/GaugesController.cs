using Microsoft.AspNetCore.Mvc;
using OdectyStat1.Contracts;
using OdectyStat1.Dto;

namespace OdectyStat1.Controllers;

[ApiController]
[Route("api/gauges")]
public class GaugesController : ControllerBase
{
    private readonly IGaugeQueryService queryService;

    public GaugesController(IGaugeQueryService queryService)
    {
        this.queryService = queryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GaugeOverviewDto>>> GetAll(CancellationToken cancellationToken)
    {
        var overview = await queryService.GetOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    [HttpGet("{id:int}/lastphoto")]
    public async Task<IActionResult> GetLastPhoto(int id, CancellationToken cancellationToken)
    {
        var photo = await queryService.GetLastPhotoAsync(id, cancellationToken);
        if (photo is null)
        {
            return NotFound();
        }

        return File(photo.Content, photo.ContentType, photo.FileName);
    }
}
