namespace OdectyStat1.Dto;
public class OdectySettings
{
    public required string RabbitMQHost { get; set; }
    public required string RabbitMQUsername { get; set; }
    public required string RabbitMQPassword { get; set; }
    public required string RabbitMQVHost { get; set; }
    public bool RabbitMQUseTls { get; set; } = true;
    public int? RabbitMQPort { get; set; }
    public string? RabbitMQTlsServerName { get; set; }
    public required string ExchangeName { get; set; }
    public List<QueueMapping> QueueMappings { get; set; } = new();
}
