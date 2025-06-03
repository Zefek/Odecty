using Microsoft.EntityFrameworkCore;
using OdectyMVC.Business;
using OdectyMVC.Contracts;
using OdectyMVC.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.DataLayer
{
    internal class MeasurementRepository : IMeasurementRepository
    {
        private readonly GaugeDbContext context;

        public MeasurementRepository(GaugeDbContext context)
        {
            this.context=context;
        }
        public async Task<ICollection<GaugeMeasurement>> Get(int gaugeId, DateTime? date)
        {
            if(date == null)
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
