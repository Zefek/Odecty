using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace OdectyStat1.Reliability;

public class GrafanaClient
{
    readonly HttpClient http;
    readonly string prometheus;
    readonly string loki;
    readonly string lokiUid;
    readonly string baseUrl;

    public GrafanaClient(ReliabilitySettings settings)
    {
        baseUrl = settings.GrafanaUrl;
        lokiUid = settings.Options.LokiUid;
        http = new HttpClient { BaseAddress = new Uri(baseUrl + "/"), Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Options.Token);
        prometheus = $"api/datasources/proxy/uid/{settings.Options.PrometheusUid}/api/v1/";
        loki = $"api/datasources/proxy/uid/{lokiUid}/loki/api/v1/";
    }

    async Task<JsonArray> Get(string path, Dictionary<string, string> query)
    {
        var url = path + "?" + string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        using var response = await http.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{(int)response.StatusCode} {path} {query["query"]}: {body}");
        }
        return (JsonArray)JsonNode.Parse(body)!["data"]!["result"]!;
    }

    string Seconds(DateTimeOffset t) => t.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

    string Nanos(DateTimeOffset t) => (t.ToUnixTimeMilliseconds() * 1_000_000).ToString(CultureInfo.InvariantCulture);

    public Task<JsonArray> PromInstant(string query, DateTimeOffset at) =>
        Get(prometheus + "query", new() { ["query"] = query, ["time"] = Seconds(at) });

    public Task<JsonArray> PromRange(string query, DateTimeOffset start, DateTimeOffset end, int step) =>
        Get(prometheus + "query_range", new() { ["query"] = query, ["start"] = Seconds(start), ["end"] = Seconds(end), ["step"] = step.ToString(CultureInfo.InvariantCulture) });

    public Task<JsonArray> LokiInstant(string query, DateTimeOffset at) =>
        Get(loki + "query", new() { ["query"] = query, ["time"] = Nanos(at) });

    public Task<JsonArray> LokiRange(string query, DateTimeOffset start, DateTimeOffset end, int step) =>
        Get(loki + "query_range", new() { ["query"] = query, ["start"] = Nanos(start), ["end"] = Nanos(end), ["step"] = $"{step}s" });

    public Task<JsonArray> LokiLogs(string query, DateTimeOffset start, DateTimeOffset end, int limit, string direction) =>
        Get(loki + "query_range", new() { ["query"] = query, ["start"] = Nanos(start), ["end"] = Nanos(end), ["limit"] = limit.ToString(CultureInfo.InvariantCulture), ["direction"] = direction });

    public string ExploreUrl(string query, DateTimeOffset start, DateTimeOffset end)
    {
        var pane = new JsonObject
        {
            ["a"] = new JsonObject
            {
                ["datasource"] = lokiUid,
                ["queries"] = new JsonArray(new JsonObject
                {
                    ["refId"] = "A",
                    ["expr"] = query,
                    ["queryType"] = "range",
                    ["datasource"] = new JsonObject { ["type"] = "loki", ["uid"] = lokiUid },
                }),
                ["range"] = new JsonObject
                {
                    ["from"] = start.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                    ["to"] = end.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                },
            },
        };
        return $"{baseUrl}/explore?schemaVersion=1&orgId=1&panes={Uri.EscapeDataString(pane.ToJsonString())}";
    }
}
