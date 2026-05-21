using OdectyStat1.Dto;

namespace OdectyStat1.Contracts;

public interface IGaugeQueryService
{
    Task<IReadOnlyList<GaugeOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<GaugePhoto?> GetLastPhotoAsync(int gaugeId, CancellationToken cancellationToken = default);
}
