using Microsoft.EntityFrameworkCore;
using OdectyStat1.Contracts;
using OdectyStat1.Entities;

namespace OdectyStat1.DataLayer
{
    internal class MeasurementStatisticsRepository : IMeasurementStatisticsRepository
    {
        private readonly GaugeDbContext context;

        public MeasurementStatisticsRepository(GaugeDbContext context)
        {
            this.context = context;
        }
        public async Task<GaugeMeasurementStatistics?> GetLast(int gaugeId)
        {
            return await context.GaugeMeasurementStatistics.Where(k => k.GaugeId == gaugeId).OrderByDescending(k => k.MeasurementDateTime).FirstOrDefaultAsync();
        }
    }
}
