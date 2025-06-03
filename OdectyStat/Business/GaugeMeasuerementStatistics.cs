using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OdectyMVC.Business;

namespace OdectyStat.Entities
{
    [Table("GaugeMeasuerementStatistics")]
    public class GaugeMeasuerementStatistics
    {
        [Key]
        public int Id { get; set; }

        public int GaugeId { get; set; }

        public DateTime MeasurementDateTime { get; set; }

        public decimal Value { get; set; }

        [NotMapped]
        public decimal InitialConsumption { get; set; } = 0;

        [NotMapped]
        public decimal InitialValue { get; set; }

        public decimal CurrentValue { get; set; }

        [NotMapped]
        public List<GaugeMeasurement> Measurements { get; set; } = new List<GaugeMeasurement>();

        public void ComputeToEndDay(GaugeMeasuerementStatistics nextDay)
        {
            var lastMasuremens = Measurements.OrderByDescending(k => k.CurrentValue).First();
            if (nextDay != null)
            {
                var endDay = lastMasuremens.MeasurementDateTime.Date.AddDays(1).AddSeconds(-1);
                var diff = nextDay.Measurements.OrderBy(k => k.CurrentValue).First() - lastMasuremens;
                var minutesToEndDay = (decimal)(endDay - lastMasuremens.MeasurementDateTime).TotalMinutes;
                var consumptionToEndDay = diff.Consumption * (minutesToEndDay / (decimal)diff.TotalDateTime.TotalMinutes);

                var nextDayFirstMeasurement = nextDay.Measurements.OrderBy(k => k.CurrentValue).First();
                var nextDayFirstMeasurementDate = nextDayFirstMeasurement.MeasurementDateTime.Date;
                var minutesFromDayBegin = (decimal)(nextDayFirstMeasurement.MeasurementDateTime - nextDayFirstMeasurementDate).TotalMinutes;

                nextDay.InitialConsumption = diff.Consumption * minutesFromDayBegin/(decimal)diff.TotalDateTime.TotalMinutes;
                nextDay.InitialValue = nextDayFirstMeasurement.CurrentValue - nextDay.InitialConsumption;
                Value = InitialConsumption + lastMasuremens.CurrentValue - Measurements.Min(k => k.CurrentValue) + consumptionToEndDay;
                CurrentValue = lastMasuremens.CurrentValue + consumptionToEndDay;
                MeasurementDateTime = endDay;
            }
            else
            {
                Value = lastMasuremens.CurrentValue - InitialValue;
                CurrentValue = lastMasuremens.CurrentValue;
                MeasurementDateTime = lastMasuremens.MeasurementDateTime;
            }
        }
    }
}
