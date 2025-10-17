using OdectyMVC.Business;

namespace OdectyStat1.Contracts
{
    public interface IGaugeRepository
    {
        Task<Gauge> GetGauge(int id);
    }
}
