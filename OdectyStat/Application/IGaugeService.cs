using OdectyStat1.Dto;

namespace OdectyStat1.Application
{
    public interface IGaugeService
    {
        Task AddIncrement(int gaugeId, decimal increment, DateTime datetime);
        Task AddNewValue(NewValue newValue);
        Task GaugeRecognizedFailed(int gaugeId, string imagePath, decimal correlationId = 0);
        Task GaugeRecognizedSucceeded(int gaugeId, string imagePath, decimal value, DateTime dateTime, decimal? confidence, decimal correlationId = 0, decimal[][]? digitProbs = null);
    }
}