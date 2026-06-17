namespace OdectyStat1.Dto;

public class FirmwareFile
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
