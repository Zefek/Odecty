using OdectyStat1.Entities;

namespace OdectyStat1.Contracts
{
    public interface IMeasurementDayRepository
    {
        Task<List<GaugeMeasurementStatistics>> GetDay(int gaugeId);
        Task SetStatistics(List<GaugeMeasurementStatistics> stats);
    }
}