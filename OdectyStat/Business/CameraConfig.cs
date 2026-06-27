namespace OdectyStat1.Business;

public class CameraConfig
{
    public long Id { get; set; }

    public int GaugeId { get; set; }

    public DateTime Timestamp { get; set; }

    public byte SchemaVer { get; set; }

    public int FwVersion { get; set; }

    public byte Framesize { get; set; }

    public byte Quality { get; set; }

    public int AecValue { get; set; }

    public byte ExposureCtrl { get; set; }

    public byte GainCtrl { get; set; }

    public byte AgcGain { get; set; }

    public sbyte AeLevel { get; set; }

    public sbyte Brightness { get; set; }

    public sbyte Contrast { get; set; }

    public sbyte Saturation { get; set; }

    public byte Whitebal { get; set; }

    public byte AwbGain { get; set; }

    public byte WbMode { get; set; }

    public byte SpecialEffect { get; set; }

    public byte Hmirror { get; set; }

    public byte Vflip { get; set; }

    public byte Aec2 { get; set; }

    public byte Gainceiling { get; set; }

    public byte Dcw { get; set; }

    public byte Bpc { get; set; }

    public byte Wpc { get; set; }

    public byte RawGma { get; set; }

    public byte Lenc { get; set; }

    public byte CfgHash { get; set; }
}
