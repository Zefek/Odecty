namespace OdectyStat1.Business;

public class ReliabilityDailyIssue
{
    public DateOnly Day { get; set; }

    public required string Fingerprint { get; set; }

    public required string Source { get; set; }

    public required string Level { get; set; }

    public required string Template { get; set; }

    public string? ExceptionType { get; set; }

    public string? Location { get; set; }

    public int Count { get; set; }

    public DateTime? FirstTs { get; set; }

    public DateTime? LastTs { get; set; }

    public int[] Hourly { get; set; } = [];

    public required string Details { get; set; }
}
