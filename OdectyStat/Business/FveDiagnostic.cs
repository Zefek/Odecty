namespace OdectyStat1.Business;

public class FveDiagnostic
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public long UptimeMinutes { get; set; }
    public int FreeHeapKb { get; set; }
    public int MinFreeHeapKb { get; set; }
    public int WifiReconnects { get; set; }
    public int MqttFailCount { get; set; }
    public int OtaFailCount { get; set; }
    public int LoopMaxMs { get; set; }
    public int BelFrameErrors { get; set; }
    public int LoadDropouts { get; set; }
    public int RawA { get; set; }
    public int RawB { get; set; }
    public int RippleA { get; set; }
    public int RippleB { get; set; }
    public byte ResetReason { get; set; }
    public int FwVersion { get; set; }
    public sbyte? Rssi { get; set; }
    public int? FanRpmA { get; set; }
    public int? FanRpmB { get; set; }
    public int? FanRunPctA { get; set; }
    public int? FanRunPctB { get; set; }
    public int? FanMismatchSlots { get; set; }
}
