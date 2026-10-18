namespace OdectyStat1.Dto;

public class MqttSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 8883;
    public bool UseTls { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string ClientId { get; set; } = "OdectyStat-reports";
}
