using Microsoft.EntityFrameworkCore;
using OdectyMVC.Business;
using OdectyMVC.Contracts;

namespace OdectyMVC.DataLayer
{
    internal class GaugeRepository : IGaugeRepository
    {
        private GaugeDbContext gaugeContext;

        public GaugeRepository(GaugeDbContext gaugeContext)
        {
            this.gaugeContext=gaugeContext;
        }

        public async Task<Gauge> GetGauge(int id)
        {
            return await gaugeContext.Gauge.FindAsync(id);
        }
    }
}