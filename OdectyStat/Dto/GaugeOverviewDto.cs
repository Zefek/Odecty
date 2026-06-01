namespace OdectyStat1.Dto;

public class GaugeOverviewDto
{
    public int Id { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal LastValue { get; set; }
    public DateTime? LastMeasurementAt { get; set; }
    public bool HasPhoto { get; set; }
}
