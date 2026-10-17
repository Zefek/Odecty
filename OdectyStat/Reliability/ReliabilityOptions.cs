namespace OdectyStat1.Reliability;

public class ReliabilityOptions
{
    public bool Enabled { get; set; }
    public string RunAt { get; set; } = "06:30";
    public bool RunOnStartup { get; set; }
    public string GrafanaUrl { get; set; } = "https://grafana.home";
    public string Token { get; set; } = "";
    public string PrometheusUid { get; set; } = "dfm89gn5z72m8d";
    public string LokiUid { get; set; } = "cfj3ab0umk0zke";
    public List<string> Apps { get; set; } = new();
    public List<string> OwnNamespaces { get; set; } = new();
    public List<ReliabilityComponent> Topology { get; set; } = new();
    public ReliabilityCommentOptions Comment { get; set; } = new();
    public ReliabilityHomeAssistantOptions HomeAssistant { get; set; } = new();
}

public class ReliabilityComponent
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Role { get; set; }
    public List<string> DependsOn { get; set; } = new();
    public string? Sends { get; set; }
    public List<string> Via { get; set; } = new();
    public List<string> Aliases { get; set; } = new();
    public List<string> Namespaces { get; set; } = new();
}

public class ReliabilityCommentOptions
{
    public bool Enabled { get; set; } = true;
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public List<string> Models { get; set; } = new();
    public string Prompt { get; set; } = Path.Combine("Reliability", "prompt", "system-prompt.md");
    public int TimeoutSeconds { get; set; } = 180;
}

public class ReliabilityHomeAssistantOptions
{
    public bool Publish { get; set; } = true;
    public string ObjectId { get; set; } = "spolehlivost_report";
    public string Name { get; set; } = "Spolehlivost report";
    public string BaseTopic { get; set; } = "odecty";
}
