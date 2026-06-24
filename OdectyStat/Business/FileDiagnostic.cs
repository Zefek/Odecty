namespace OdectyStat1.Business;

public class FileDiagnostic
{
    public long Id { get; set; }

    public decimal CorrelationId { get; set; }

    public int GaugeId { get; set; }

    public DateTime Timestamp { get; set; }

    public string? FilePath { get; set; }

    public bool Success { get; set; }

    public decimal? RecognizedValue { get; set; }

    public decimal? CorrectedValue { get; set; }

    public decimal? Confidence { get; set; }
}
