namespace OdectyStat1.Business;

public class DeviceDiagnostic
{
    public long Id { get; set; }

    public int GaugeId { get; set; }

    public DateTime Timestamp { get; set; }

    public byte SchemaVer { get; set; }

    public long UptimeSeconds { get; set; }

    public int FwVersion { get; set; }

    public int FreeHeapKb { get; set; }

    public int MinFreeHeapKb { get; set; }

    public int MaxAllocKb { get; set; }

    public int WifiReconnects { get; set; }

    public int CaptureCount { get; set; }

    public int SendFailures { get; set; }

    public int TlsErrors { get; set; }

    public int OtaFailures { get; set; }

    public int LoopMaxMs { get; set; }

    public byte CameraErrors { get; set; }

    public byte ResetReason { get; set; }

    public sbyte Rssi { get; set; }

    public byte CfgHash { get; set; }
}
