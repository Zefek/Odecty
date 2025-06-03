using OdectyMVC.Contracts;
using OdectyStat;
using OdectyStat.Business;
using OdectyStat.Dto;
using OdectyStat1.Business;

namespace OdectyMVC.Application
{
    public class GaugeService : IGaugeService
    {
        private readonly IGaugeContext context;

        public GaugeService(IGaugeContext context)
        {
            this.context=context;
        }

        public async Task AddNewValue(NewValue newValue)
        {
            Console.WriteLine("Add new value " + newValue.GaugeId);
            var gauge = await context.GaugeRepository.GetGauge(newValue.GaugeId);
            gauge.SetNewValue(newValue.Value, newValue.Datetime);
            await context.SaveChangesAsync();

            var service = new ComputeService3(context);
            var result = await service.Compute(newValue.GaugeId);
            context.AddRange(result);

            if (gauge.HomeassistantId.HasValue)
            {
                var homeStatistics = await context.HomeAssistantStatisticsRepository.GetForGauge(gauge.HomeassistantId.Value);
                var shortTerm = await context.HomeAssistantStatisticsRepository.GetForStatisticsShortTerm(gauge.HomeassistantId.Value);
                foreach (var r in result)
                {
                    var time = new DateTimeOffset(r.MeasurementDateTime.Date.AddDays(1)).ToUnixTimeSeconds();
                    var timeStartDay = new DateTimeOffset(r.MeasurementDateTime.Date).ToUnixTimeSeconds();
                    var lastDayRecord = homeStatistics.Where(k => k.StartTs >= timeStartDay && k.StartTs < time).OrderByDescending(k => k.StartTs).FirstOrDefault();
                    var lastDayShort = shortTerm.Where(k => k.StartTs >= timeStartDay && k.StartTs < time).OrderByDescending(k => k.StartTs).FirstOrDefault();
                    if (lastDayRecord != null)
                    {
                        lastDayRecord.State = Math.Round((double)r.Value, 4);
                    }
                    else
                    {
                        lastDayRecord = new Statistic
                        {
                            StartTs = new DateTimeOffset(r.MeasurementDateTime.Date.AddHours(20)).ToUnixTimeSeconds(),
                            CreatedTs = new DateTimeOffset(r.MeasurementDateTime.Date.AddHours(20)).ToUnixTimeSeconds(),
                            State = (double)r.Value,
                            MetadataId = gauge.HomeassistantId.Value
                        };
                        homeStatistics.Add(lastDayRecord);
                        context.AddHomeAssistant(lastDayRecord);
                    }
                    if(lastDayShort != null)
                    {
                        lastDayShort.State = Math.Round((double)r.Value, 4);
                    }
                }
                double sum = 0;
                foreach (var s in homeStatistics.OrderBy(k=>k.StartTs))
                {
                    sum+=s.State ?? 0;
                    s.Sum = Math.Round(sum, 4);
                }
                
                foreach (var s in shortTerm.OrderByDescending(k => k.StartTs))
                {
                    s.Sum = Math.Round(sum, 4);
                    sum-=s.State ?? 0;
                }
            }
            await context.SaveChangesAsync();

            //context.ExcelProvider.UpdateExcel();
        }

        public async Task AddIncrement(int gaugeId, decimal increment, DateTime datetime)
        {
            var gauge = await context.GaugeRepository.GetGauge(gaugeId);
            gauge.AddIncrement(increment, datetime);
            await context.SaveChangesAsync();
        }
    }
}
