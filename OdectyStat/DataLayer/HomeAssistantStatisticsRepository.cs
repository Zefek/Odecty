using Microsoft.EntityFrameworkCore;
using OdectyMVC.Business;
using OdectyMVC.Contracts;
using OdectyStat1.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.DataLayer
{
    internal class HomeAssistantStatisticsRepository : IHomeAssistantStatisticsRepository
    {
        private readonly HomeAssistantDbContext context;

        public HomeAssistantStatisticsRepository(HomeAssistantDbContext context)
        {
            this.context=context;
        }
        public async Task<ICollection<Statistic>> GetForGauge(int homeassistantId)
        {
            return await context.Statistics.Where(k => k.MetadataId == homeassistantId).ToListAsync();
        }

        public async Task<ICollection<StatisticsShortTerm>> GetForStatisticsShortTerm(int homeassistantId)
        {
            return await context.StatisticsShortTerms.Where(k=>k.MetadataId == homeassistantId).ToListAsync();
        }
    }
}
