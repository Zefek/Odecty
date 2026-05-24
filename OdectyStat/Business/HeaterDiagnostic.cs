namespace OdectyStat1.Business;

public class HeaterDiagnostic
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public long UptimeMinutes { get; set; }
    public int FreeRam { get; set; }
    public int WifiReconnects { get; set; }
    public int MqttReconnects { get; set; }
    public int SensorErrors { get; set; }
    public byte ResetReason { get; set; }
    public int LoopMaxMs { get; set; }
    public sbyte? Rssi { get; set; }
}
