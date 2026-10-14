using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdectyStat1.Business
{
    [Table("BillingPeriod")]
    public class BillingPeriod
    {
        [Key]
        public int Id { get; set; }

        public int GaugeId { get; set; }

        public DateOnly PeriodFrom { get; set; }

        public DateOnly PeriodTo { get; set; }

        public decimal StartValue { get; set; }

        public decimal EndValue { get; set; }

        public decimal? VolumeCoefficient { get; set; }

        public decimal? CalorificValue { get; set; }
    }
}
