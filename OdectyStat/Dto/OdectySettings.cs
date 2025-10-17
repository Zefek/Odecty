namespace OdectyStat1.Dto;
public class OdectySettings
{
    public string RabbitMQHost { get; set; }
    public string RabbitMQUsername { get; set; }
    public string RabbitMQPassword { get; set; }
    public string RabbitMQVHost { get; set; }
    public string ExchangeName { get; set; }
    public List<QueueMapping> QueueMappings { get; set; }
}
