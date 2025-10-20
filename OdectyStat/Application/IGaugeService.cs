using OdectyStat1.Dto;

namespace OdectyStat1.Application
{
    public interface IGaugeService
    {
        Task AddIncrement(int gaugeId, decimal increment, DateTime datetime);
        Task AddNewValue(NewValue newValue);
        void GaugeRecognizedFailed(int gaugeId, string imagePath);
        Task GaugeRecognizedSucceeded(int gaugeId, string imagePath, decimal value);
    }
}