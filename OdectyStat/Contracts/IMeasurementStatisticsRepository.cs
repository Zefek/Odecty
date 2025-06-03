using OdectyStat.Entities;

namespace OdectyMVC.Contracts
{
    public interface IMeasurementStatisticsRepository
    {
        Task<GaugeMeasuerementStatistics> GetLast(int gaugeId);
    }
}