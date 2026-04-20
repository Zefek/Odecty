using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odecty.SMVAK;

[Table("Gauge")]
internal class Gauge
{
    public int Id { get; set; }
}

[Table("GaugeMeasurement")]
internal class GaugeMeasurement
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Gauge))]
    public int GaugeId { get; set; }
    public Gauge Gauge { get; set; }

    public DateTime MeasurementDateTime { get; set; }

    public decimal Value { get; set; }

    public decimal CurrentValue { get; set; }
    public string? ImagePath { get; set; }
    public DateTime LastMeasurementDateTime { get; set; }
}
