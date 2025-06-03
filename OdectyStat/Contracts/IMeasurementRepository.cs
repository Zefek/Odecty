
using OdectyMVC.Business;

namespace OdectyMVC.Contracts
{
    public interface IMeasurementRepository
    {
        Task<ICollection<GaugeMeasurement>> Get(int gaugeId, DateTime? date);
    }
}