namespace OdectyStat1.Business;

public class ReliabilityDailyMetric
{
    public DateOnly Day { get; set; }

    public required string Source { get; set; }

    public required string Metric { get; set; }

    public string Key { get; set; } = "";

    public double Value { get; set; }
}
