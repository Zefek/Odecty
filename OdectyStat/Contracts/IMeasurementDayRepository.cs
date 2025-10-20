using OdectyStat1.Entities;

namespace OdectyStat1.Contracts
{
    public interface IMeasurementDayRepository
    {
        Task<List<GaugeMeasuerementStatistics>> GetDay(int gaugeId);
        Task SetStatistics(List<GaugeMeasuerementStatistics> stats);
    }
}