using OdectyStat1.Business;

namespace OdectyStat1.Contracts
{
    public interface IHomeAssistantStatisticsRepository
    {
        Task<ICollection<Statistic>> GetForGauge(int homeassistantId);
        Task<ICollection<StatisticsShortTerm>> GetForStatisticsShortTerm(int homeassistantId);
    }
}