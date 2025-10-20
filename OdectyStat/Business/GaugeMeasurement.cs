using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdectyStat1.Business
{
    [Table("GaugeMeasurement")]
    public class GaugeMeasurement
    {
        [Key]
        public int Id { get; set; }

        public int GaugeId { get; set; }

        public DateTime MeasurementDateTime { get; set; }

        public decimal Value { get; set; }

        public decimal CurrentValue { get; set; }
        public string? ImagePath { get; set; }

        public static MeasurementDiff operator -(GaugeMeasurement first, GaugeMeasurement second)
        {
            return new MeasurementDiff
            {
                Consumption = first.CurrentValue - second.CurrentValue,
                TotalDateTime = first.MeasurementDateTime - second.MeasurementDateTime
            };
        }
    }
}
