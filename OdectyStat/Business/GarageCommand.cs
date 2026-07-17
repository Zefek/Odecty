namespace OdectyStat1.Business;

public class GarageCommand
{
    public long Id { get; set; }
    public long CorrelationId { get; set; }
    public required string Identity { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "pending";
}
