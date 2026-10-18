using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdectyStat1.Business
{
    [Table("Tariff")]
    public class Tariff
    {
        [Key]
        public int Id { get; set; }

        public int GaugeId { get; set; }

        public DateOnly ValidFrom { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal MonthlyFee { get; set; }
    }
}
