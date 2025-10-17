using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer
{
    internal class GaugeRepository : IGaugeRepository
    {
        private GaugeDbContext gaugeContext;

        public GaugeRepository(GaugeDbContext gaugeContext)
        {
            this.gaugeContext = gaugeContext;
        }

        public async Task<Gauge> GetGauge(int id)
        {
            return await gaugeContext.Gauge.FindAsync(id);
        }
    }
}