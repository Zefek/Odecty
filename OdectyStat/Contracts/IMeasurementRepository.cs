
using OdectyMVC.Business;

namespace OdectyStat1.Contracts
{
    public interface IMeasurementRepository
    {
        Task<ICollection<GaugeMeasurement>> Get(int gaugeId, DateTime? date);
    }
}