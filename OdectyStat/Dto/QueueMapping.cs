namespace OdectyStat1.Dto;
public class QueueMapping
{
    public required string QueueName { get; set; }
    public string? RoutingKey { get; set; }
    public required string ExchangeName { get; set; }
}
