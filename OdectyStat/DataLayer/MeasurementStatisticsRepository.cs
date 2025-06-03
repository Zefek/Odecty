using Microsoft.EntityFrameworkCore;
using OdectyMVC.Contracts;
using OdectyMVC.DataLayer;
using OdectyStat.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.DataLayer
{
    internal class MeasurementStatisticsRepository : IMeasurementStatisticsRepository
    {
        private readonly GaugeDbContext context;

        public MeasurementStatisticsRepository(GaugeDbContext context)
        {
            this.context=context;
        }
        public async Task<GaugeMeasuerementStatistics> GetLast(int gaugeId)
        {
            return await context.GaugeMeasuerementStatistics.Where(k=>k.GaugeId == gaugeId).OrderByDescending(k=>k.MeasurementDateTime).FirstOrDefaultAsync();
        }
    }
}
