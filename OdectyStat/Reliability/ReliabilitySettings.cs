namespace OdectyStat1.Reliability;

public class ReliabilitySettings
{
    public ReliabilityOptions Options { get; }
    public TimeZoneInfo Zone { get; } = TimeZoneInfo.Local;
    public string[] Apps { get; }
    public string[] OwnNamespaces { get; }
    public string[] CommentModels { get; }
    public string PromptFile { get; }

    public ReliabilitySettings(ReliabilityOptions options)
    {
        Options = options;
        Apps = options.Apps.Count > 0 ? options.Apps.ToArray() : ["OdectyStat", "OdectyMVC", "TemperatureSensorArduinoReader"];
        OwnNamespaces = options.OwnNamespaces.Count > 0 ? options.OwnNamespaces.ToArray() : ["OdectyStat1", "OdectyMVC", "TemperatureSensorArduinoReader"];
        CommentModels = options.Comment.Models.Count > 0 ? options.Comment.Models.ToArray() : ["gemma4:31b-cloud", "nemotron-3-ultra:cloud"];
        PromptFile = Path.GetFullPath(options.Comment.Prompt, AppContext.BaseDirectory);
    }

    public string GrafanaUrl => Options.GrafanaUrl.TrimEnd('/');

    public string OllamaUrl => Options.Comment.OllamaUrl.TrimEnd('/');

    public DateOnly Yesterday => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.Now, Zone).DateTime).AddDays(-1);
}
