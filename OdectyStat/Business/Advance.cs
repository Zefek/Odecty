using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdectyStat1.Business
{
    [Table("Advance")]
    public class Advance
    {
        [Key]
        public int Id { get; set; }

        public int GaugeId { get; set; }

        public DateOnly ValidFrom { get; set; }

        public decimal MonthlyAmount { get; set; }
    }
}
