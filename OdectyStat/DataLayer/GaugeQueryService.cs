using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OdectyStat1.Contracts;
using OdectyStat1.Dto;

namespace OdectyStat1.DataLayer;

internal class GaugeQueryService : IGaugeQueryService
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly GaugeDbContext context;
    private readonly IOptions<GaugeImageLocation> imageLocation;

    public GaugeQueryService(GaugeDbContext context, IOptions<GaugeImageLocation> imageLocation)
    {
        this.context = context;
        this.imageLocation = imageLocation;
    }

    public async Task<IReadOnlyList<GaugeOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        return await context.Gauge
            .AsNoTracking()
            .Select(g => new GaugeOverviewDto
            {
                Id = g.Id,
                Description = g.Description,
                Type = g.Type,
                LastValue = g.LastValue,
                LastMeasurementAt = context.GaugeMeasurement
                    .Where(m => m.GaugeId == g.Id)
                    .OrderByDescending(m => m.MeasurementDateTime)
                    .Select(m => (DateTime?)m.MeasurementDateTime)
                    .FirstOrDefault(),
                HasPhoto = context.GaugeMeasurement
                    .Where(m => m.GaugeId == g.Id)
                    .OrderByDescending(m => m.MeasurementDateTime)
                    .Select(m => m.ImagePath)
                    .FirstOrDefault() != null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GaugePhoto?> GetLastPhotoAsync(int gaugeId, CancellationToken cancellationToken = default)
    {
        var metadata = await context.GaugeMeasurement
            .AsNoTracking()
            .Where(m => m.GaugeId == gaugeId && m.ImagePath != null)
            .OrderByDescending(m => m.MeasurementDateTime)
            .Select(m => new { m.ImagePath, m.MeasurementDateTime })
            .FirstOrDefaultAsync(cancellationToken);

        if (metadata is null)
        {
            return null;
        }

        var path = ResolvePhotoPath(gaugeId, metadata.ImagePath!, metadata.MeasurementDateTime);
        if (path is null)
        {
            return null;
        }

        if (!ContentTypeProvider.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new GaugePhoto
        {
            Content = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read),
            ContentType = contentType,
            FileName = Path.GetFileName(path)
        };
    }

    private string? ResolvePhotoPath(int gaugeId, string imagePath, DateTime measurementDateTime)
    {
        var gaugeFolder = string.Format(imageLocation.Value.RecognizedSuccessFolder, gaugeId);

        // Nový formát: ImagePath obsahuje i datovou podsložku (yyyy-MM-dd/soubor.jpg) → použij přímo.
        if (imagePath.Contains('/') || imagePath.Contains('\\'))
        {
            var direct = Path.Combine(gaugeFolder, imagePath);
            return File.Exists(direct) ? direct : null;
        }

        // Starý formát: jen název souboru → datovou složku odhadneme podle data měření (±1 den).
        var measurementDate = measurementDateTime.Date;
        foreach (var offset in new[] { 0, -1, 1 })
        {
            var dateFolder = measurementDate.AddDays(offset).ToString("yyyy-MM-dd");
            var candidate = Path.Combine(gaugeFolder, dateFolder, imagePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
