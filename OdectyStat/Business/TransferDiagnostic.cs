namespace OdectyStat1.Business;

public class TransferDiagnostic
{
    public long Id { get; set; }

    public decimal CorrelationId { get; set; }

    public int GaugeId { get; set; }

    public DateTime Timestamp { get; set; }

    public byte SchemaVer { get; set; }

    public long ImgSize { get; set; }

    public long BytesSent { get; set; }

    public long DurationMs { get; set; }

    public byte TryCount { get; set; }

    public bool Success { get; set; }

    public short HttpCode { get; set; }

    public sbyte Rssi { get; set; }
}
