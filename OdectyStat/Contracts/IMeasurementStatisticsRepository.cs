using OdectyStat1.Entities;

namespace OdectyStat1.Contracts
{
    public interface IMeasurementStatisticsRepository
    {
        Task<GaugeMeasuerementStatistics> GetLast(int gaugeId);
    }
}