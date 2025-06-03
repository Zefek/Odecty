using OdectyStat.Dto;

namespace OdectyMVC.Application
{
    public interface IGaugeService
    {
        Task AddIncrement(int gaugeId, decimal increment, DateTime datetime);
        Task AddNewValue(NewValue newValue);
    }
}