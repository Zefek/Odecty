using Microsoft.EntityFrameworkCore;
using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer
{
    internal class MeasurementRepository : IMeasurementRepository
    {
        private readonly GaugeDbContext context;

        public MeasurementRepository(GaugeDbContext context)
        {
            this.context = context;
        }
        public async Task<ICollection<GaugeMeasurement>> Get(int gaugeId, DateTime? date)
        {
            if (date == null)
            {
                return await context.GaugeMeasurement.Where(k => k.GaugeId == gaugeId)
                .OrderBy(k => k.MeasurementDateTime)
                .ToListAsync();

            }
            return await context.GaugeMeasurement.Where(k => k.GaugeId == gaugeId && k.MeasurementDateTime > date)
                .OrderBy(k => k.MeasurementDateTime)
                .ToListAsync();
        }
    }
}
