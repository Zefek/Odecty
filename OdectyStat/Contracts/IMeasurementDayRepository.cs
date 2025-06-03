using OdectyStat.Entities;

namespace OdectyStat.Contracts
{
    public interface IMeasurementDayRepository
    {
        Task<List<GaugeMeasuerementStatistics>> GetDay(int gaugeId);
        Task SetStatistics(List<GaugeMeasuerementStatistics> stats);
    }
}