using Microsoft.EntityFrameworkCore;
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
            var gauge = await gaugeContext.Gauge.FindAsync(id);
            gauge.LastMeasurement = await gaugeContext.GaugeMeasurement.Where(k => k.GaugeId == id).OrderByDescending(k => k.MeasurementDateTime).FirstOrDefaultAsync();
            return gauge;
        }
    }
}