using OdectyMVC.Contracts;
using OdectyStat.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.Business
{
    internal class ComputeService3
    {
        private readonly IGaugeContext context;

        public ComputeService3(IGaugeContext context)
        {
            this.context=context;
        }

        public async Task<ICollection<GaugeMeasuerementStatistics>> Compute(int gaugeId)
        {
            var lastStat = await context.MeasurementStatisticsRepository.GetLast(gaugeId);
            var measurement = await context.MeasurementRepository.Get(gaugeId, lastStat?.MeasurementDateTime);
            //5.5.2025 23:59:59
            var day = lastStat?.MeasurementDateTime.Date.AddDays(2).AddSeconds(-1) ?? measurement.First().MeasurementDateTime.Date.AddDays(1).AddSeconds(-1);
            //6.5.2025 23:59:59
            var msrGrp = measurement.GroupBy(k => k.MeasurementDateTime.Date);
            var lastStates = new List<GaugeMeasuerementStatistics>();
            while (msrGrp.Any(k => k.Key > day))
            {
                var currentDay = msrGrp.FirstOrDefault(k => k.Key == day.Date);
                var nextDay = msrGrp.First(k => k.Key > day.Date);
                if(day.Date == new DateTime(2023, 6, 13, 0, 0, 0))
                {

                }
                var lastCurrentDayMeasurement = currentDay?.OrderByDescending(k => k.MeasurementDateTime).First();

                decimal consumption = 0;
                if(lastStat!= null)
                {
                    consumption = nextDay.Select(k => k.CurrentValue).Min() - ((currentDay?.Select(k => k.CurrentValue).Max()) ?? lastStat.CurrentValue);
                }
                else
                {
                    consumption = nextDay.Select(k => k.CurrentValue).Min() - currentDay.Select(k => k.CurrentValue).Max();
                }

                var diff = nextDay.OrderBy(k => k.MeasurementDateTime).First().MeasurementDateTime - ((lastCurrentDayMeasurement?.MeasurementDateTime) ?? lastStat?.MeasurementDateTime);

                var consumptionPerMinute = Math.Round( consumption / Math.Round((decimal)diff?.TotalMinutes, 4), 4);

                //Pokud je currentDay, pak se měření počítá jako poslední odečet toho den - mínus vypočítaný odečet lastStat + počet minut do konce dne (od posledního odečtu) * consumptionPerMinute
                //Pokud currentDay == null, pak se měření počítá jako počet minut lastStat - day * consumptionPerMinute
                if(lastStat == null)
                {
                    var consToEndDay = (consumptionPerMinute * (decimal)(day - currentDay.Last().MeasurementDateTime).TotalMinutes);
                    lastStat = new GaugeMeasuerementStatistics
                    {
                        MeasurementDateTime = day,
                        Value = currentDay.Last().CurrentValue - currentDay.First().CurrentValue + consToEndDay,
                        CurrentValue = currentDay.Last().CurrentValue + consToEndDay,
                        GaugeId = gaugeId
                    };
                    lastStates.Add(lastStat);
                }
                else if(currentDay != null)
                {
                    var minutesToEndDay = (day.Date.AddDays(1).AddSeconds(-1) - lastCurrentDayMeasurement.MeasurementDateTime).TotalMinutes;
                    var consumptionToEndDay = lastCurrentDayMeasurement.CurrentValue - lastStat.CurrentValue + ((decimal)minutesToEndDay * consumptionPerMinute);
                    var ls = new GaugeMeasuerementStatistics
                    {
                        MeasurementDateTime = day,
                        Value = consumptionToEndDay,
                        CurrentValue = lastStat.CurrentValue + consumptionToEndDay,
                        GaugeId = gaugeId
                    };
                    if(ls.Value < 0)
                    {

                    }
                    lastStates.Add(ls);
                    lastStat = ls;
                }
                else
                {
                    var minutesToEndDay = (day - lastStat.MeasurementDateTime).TotalMinutes;
                    var consumptionToEndDay = (decimal)minutesToEndDay * consumptionPerMinute;
                    var ls = new GaugeMeasuerementStatistics
                    {
                        MeasurementDateTime = day,
                        Value = consumptionToEndDay,
                        CurrentValue = lastStat.CurrentValue + consumptionToEndDay,
                        GaugeId = gaugeId
                    };
                    if (ls.Value < 0)
                    {

                    }
                    lastStates.Add(ls);
                    lastStat = ls;
                }
                day = day.AddDays(1);
            }
            return lastStates;
        }
    }
}
