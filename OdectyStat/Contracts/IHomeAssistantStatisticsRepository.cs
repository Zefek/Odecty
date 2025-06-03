using OdectyMVC.Business;
using OdectyStat1.Business;

namespace OdectyMVC.Contracts
{
    public interface IHomeAssistantStatisticsRepository
    {
        Task<ICollection<Statistic>> GetForGauge(int homeassistantId);
        Task<ICollection<StatisticsShortTerm>> GetForStatisticsShortTerm(int homeassistantId);
    }
}