using OdectyStat1.Entities;

namespace OdectyStat1.Contracts
{
    public interface IMeasurementStatisticsRepository
    {
        Task<GaugeMeasurementStatistics?> GetLast(int gaugeId);
    }
}