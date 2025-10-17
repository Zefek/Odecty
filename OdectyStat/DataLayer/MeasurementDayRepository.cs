using Microsoft.EntityFrameworkCore;
using OdectyStat1.Contracts;
using OdectyStat1.Entities;

namespace OdectyStat1.DataLayer
{
    public class MeasurementDayRepository : IMeasurementDayRepository
    {
        private readonly GaugeDbContext context;

        public MeasurementDayRepository(GaugeDbContext context)
        {
            this.context = context;
        }

        public async Task<List<GaugeMeasuerementStatistics>> GetDay(int gaugeId)
        {
            var measurements = await context.GaugeMeasurement.AsNoTracking().Where(k => k.GaugeId == gaugeId).ToListAsync();
            var days = measurements.GroupBy(k => k.MeasurementDateTime.Date);
            var minday = days.Min(k => k.Key);
            var maxday = days.Max(k => k.Key);
            var stats = await context.GaugeMeasuerementStatistics.AsNoTracking().Where(k => k.GaugeId == gaugeId && k.MeasurementDateTime.Date >= minday && k.MeasurementDateTime.Date <= maxday).ToListAsync();

            foreach (var day in days)
            {
                var stat = stats.FirstOrDefault(k => k.MeasurementDateTime.Date == day.Key);
                if (stat == null)
                {
                    stats.Add(new GaugeMeasuerementStatistics
                    {
                        GaugeId = day.First().GaugeId,
                        Measurements = day.ToList(),
                        MeasurementDateTime = day.OrderByDescending(k => k.MeasurementDateTime).First().MeasurementDateTime
                    });
                }
                else
                {
                    stat.Measurements = day.ToList();
                }
            }
            return stats.Where(k => k.Measurements.Any()).ToList();
        }

        public async Task SetStatistics(List<GaugeMeasuerementStatistics> stats)
        {
            stats.ForEach(k => k.Id = 0);
            var minDate = stats.Min(k => k.MeasurementDateTime);
            var maxDate = stats.Max(k => k.MeasurementDateTime.AddSeconds(1));
            var gaugeId = stats.First().GaugeId;
            var toDelete = await context.GaugeMeasuerementStatistics.Where(k => k.GaugeId == gaugeId && k.MeasurementDateTime >= minDate && k.MeasurementDateTime <= maxDate).ToListAsync();
            context.RemoveRange(toDelete);
            await context.AddRangeAsync(stats);
        }
    }
}
