namespace OdectyStat1.Business;

public class ReliabilityDailyReport
{
    public DateOnly Day { get; set; }

    public required string Status { get; set; }

    public required string Facts { get; set; }

    public string? Comment { get; set; }

    public string? CommentModel { get; set; }

    public int? CommentTokensIn { get; set; }

    public int? CommentTokensOut { get; set; }

    public DateTime CreatedAt { get; set; }
}
