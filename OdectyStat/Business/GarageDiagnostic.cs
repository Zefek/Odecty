namespace OdectyStat1.Business;

public class GarageDiagnostic
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public long UptimeMinutes { get; set; }
    public int FreeRam { get; set; }
    public int WifiReconnects { get; set; }
    public int MqttReconnects { get; set; }
    public byte SensorErrors { get; set; }
    public byte ResetReason { get; set; }
    public int LoopMaxMs { get; set; }
    public int DoorCycles { get; set; }
    public sbyte? Rssi { get; set; }
}
