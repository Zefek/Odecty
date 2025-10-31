using OdectyStat1.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdectyStat1.Business
{
    [Table("Gauge")]
    public class Gauge
    {
        [Key]
        public int Id { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal LastValue { get; set; }

        [NotMapped]
        public GaugeMeasurement LastMeasurement { get; set; }
        public decimal? MaxValuePerHour { get; set; }
        public decimal InitialValue { get; set; }

        public ICollection<GaugeMeasurement> Measurements { get; set; } = new List<GaugeMeasurement>();
        public ICollection<GaugeMeasuerementStatistics> MeasurementsStatistics { get; set; } = new List<GaugeMeasuerementStatistics>();
        public int? HomeassistantId { get; internal set; }

        public void SetNewValue(decimal newValue, DateTime datetime, string? imagePath = null)
        {
            var increment = newValue - LastValue;
            if (increment > 0)
            {
                Measurements.Add(new GaugeMeasurement
                {
                    Value = increment,
                    MeasurementDateTime = datetime,
                    LastMeasurementDateTime = datetime,
                    CurrentValue = newValue,
                    ImagePath = imagePath
                });
                LastValue = newValue;
            }
        }

        public void AddIncrement(decimal increment, DateTime datetime)
        {
            var newValue = LastValue += increment;
            Measurements.Add(new GaugeMeasurement { Value = increment, MeasurementDateTime = datetime, CurrentValue = newValue });
            LastValue = newValue;
        }

        public void SetStatistics(IEnumerable<GaugeMeasuerementStatistics> statistics)
        {
            var stats = MeasurementsStatistics.Where(k => k.GaugeId == statistics.First().GaugeId).AsEnumerable().Where(k => k.MeasurementDateTime >= statistics.Min(k => k.MeasurementDateTime) && k.MeasurementDateTime <= statistics.Max(k => k.MeasurementDateTime)).ToList();
            foreach (var stat in stats)
            {
                MeasurementsStatistics.Remove(stat);
            }
            foreach (var stat in statistics)
            {
                MeasurementsStatistics.Add(stat);
            }
        }
    }
}
