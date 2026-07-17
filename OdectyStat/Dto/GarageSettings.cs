namespace OdectyStat1.Dto;

public class GarageSettings
{
    public required string SigningKeyHex { get; set; }
    public int SignatureBytes { get; set; } = 16;
    public required string RequestTopic { get; set; }
    public required string ResponseTopic { get; set; }
    public int SlotTtlSeconds { get; set; } = 20;
}
