namespace OdectyStat1.Dto;

public class GaugePhoto
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
