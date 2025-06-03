using OdectyMVC.Business;

namespace OdectyMVC.Contracts
{
    public interface IGaugeRepository
    {
        Task<Gauge> GetGauge(int id);
    }
}
