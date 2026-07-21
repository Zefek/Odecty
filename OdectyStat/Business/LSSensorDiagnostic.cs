namespace OdectyStat1.Business;

public class LSSensorDiagnostic
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public long UptimeMinutes { get; set; }
    public int FreeRam { get; set; }
    public int WifiReconnects { get; set; }
    public int MqttFailCount { get; set; }
    public byte ResetReason { get; set; }
    public int LoopMaxMs { get; set; }
    public sbyte? Rssi { get; set; }
    public int? FwVersion { get; set; }
    public int? OtaFailCount { get; set; }
}
