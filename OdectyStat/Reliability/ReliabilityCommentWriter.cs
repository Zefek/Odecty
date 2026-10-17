using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OdectyStat1.Reliability;

public record ReliabilityComment(string Text, string Model, int? TokensIn, int? TokensOut, List<string> Attempts);

public class ReliabilityCommentWriter
{
    const string Template = "template";
    const string Placeholder = "[host]";
    const string Level = "level";
    const string Http5xx = "http_5xx";

    readonly ReliabilitySettings settings;
    readonly HttpClient http;
    readonly TimeSpan regexTimeout = TimeSpan.FromSeconds(1);
    readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    readonly string[] keptParameters = ["RequestPath", "gaugeId", "QueueName", "DeviceName", "HealthCheckName"];
    readonly (string Pattern, string Replacement)[] anonymization =
    [
        (@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", "[guid]"),
        (@"\b\d{1,3}(?:\.\d{1,3}){3}\b", "[ip]"),
        (@"\blocalhost\b", Placeholder),
        (@"\b[\w-]+(?:\.[\w-]+)*\.(?:home|local|lan|internal|cz|com|net|org)\b", Placeholder),
        (@"(?i)\b([A-Z]:\\Users\\)[^\\\s""']+", "$1[user]"),
        (@"(?i)\b(server|database|host|username|user id|password)(\s*[=:]\s*|\s+')(?!\{)[^;'\s]+", "$1$2[x]"),
    ];

    readonly List<(Regex Pattern, string Id)> masks;
    readonly Dictionary<string, string> names;
    readonly Regex? idPattern;

    public ReliabilityCommentWriter(ReliabilitySettings settings)
    {
        this.settings = settings;
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(settings.Options.Comment.TimeoutSeconds) };
        var topology = settings.Options.Topology.Where(c => c.Id.Length > 0).ToList();
        masks = topology
            .SelectMany(c => c.Namespaces.Concat(c.Aliases).Append(c.Name).Where(n => n.Length > 0).Select(n => (Text: n, c.Id)))
            .OrderByDescending(m => m.Text.Length)
            .Select(m => (new Regex($@"(?<![\w-]){Regex.Escape(m.Text)}(?![a-z0-9_-])", RegexOptions.None, regexTimeout), m.Id))
            .ToList();
        names = topology.ToDictionary(c => c.Id, c => c.Name.Length > 0 ? c.Name : c.Id);
        var prefixes = topology.Select(c => Regex.Replace(c.Id, @"\d+$", "", RegexOptions.None, regexTimeout)).Where(p => p.Length > 0).Distinct().Select(Regex.Escape).ToList();
        idPattern = prefixes.Count > 0 ? new Regex($@"\b(?:{string.Join("|", prefixes)})\d+\b", RegexOptions.None, regexTimeout) : null;
    }

    public async Task<ReliabilityComment> Write(JsonObject facts)
    {
        var attempts = new List<string>();
        if (!File.Exists(settings.PromptFile))
        {
            attempts.Add($"prompt {settings.PromptFile} nenalezen");
            return Fallback(facts, attempts);
        }
        var system = await File.ReadAllTextAsync(settings.PromptFile);
        var input = Condense(facts).ToJsonString(jsonOptions);
        foreach (var model in settings.CommentModels)
        {
            try
            {
                var (text, tokensIn, tokensOut) = await Ask(model, system, input);
                var problem = Validate(text);
                if (problem != null)
                {
                    attempts.Add($"{model}: {problem}");
                    continue;
                }
                attempts.Add($"{model}: ok");
                return new ReliabilityComment(Restore(text), model, tokensIn, tokensOut, attempts);
            }
            catch (Exception ex)
            {
                attempts.Add($"{model}: {ex.GetType().Name} {ex.Message}");
            }
        }
        return Fallback(facts, attempts);
    }

    async Task<(string Text, int? TokensIn, int? TokensOut)> Ask(string model, string system, string input)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["stream"] = false,
            ["think"] = false,
            ["options"] = new JsonObject { ["num_ctx"] = 8192, ["temperature"] = 0.3 },
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = system },
                new JsonObject { ["role"] = "user", ["content"] = input }),
        };
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync($"{settings.OllamaUrl}/api/chat", content);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {json}");
        }
        var result = JsonNode.Parse(json)!;
        var text = result["message"]?["content"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(text) && result["done_reason"]?.GetValue<string>() == "stop")
        {
            text = result["message"]?["thinking"]?.GetValue<string>() ?? "";
        }
        return (text.Trim(), result["prompt_eval_count"]?.GetValue<int>(), result["eval_count"]?.GetValue<int>());
    }

    string? Validate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "prazdna odpoved";
        }
        if (text.Length > 3000)
        {
            return "prilis dlouha odpoved";
        }
        if (text.Contains("<think>", StringComparison.OrdinalIgnoreCase))
        {
            return "odpoved obsahuje premysleni";
        }
        if (!Regex.IsMatch(text, @"\*\*[^*\r\n]+:\*\*", RegexOptions.None, regexTimeout))
        {
            return "chybi format **nadpis:**";
        }
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        if (lines > 8)
        {
            return $"prilis mnoho radku ({lines})";
        }
        var unknown = idPattern?.Matches(text).Select(m => m.Value).Where(id => !names.ContainsKey(id)).Distinct().ToList() ?? [];
        if (unknown.Count > 0)
        {
            return $"nezname id {string.Join(", ", unknown)}";
        }
        var leaked = masks.FirstOrDefault(m => m.Pattern.IsMatch(text));
        return leaked.Pattern != null ? $"obsahuje skutecny nazev ({leaked.Id})" : null;
    }

    string Restore(string text)
    {
        foreach (var (id, name) in names.OrderByDescending(n => n.Key.Length))
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(id)}\b", name, RegexOptions.None, regexTimeout);
        }
        return text;
    }

    string Mask(string text)
    {
        foreach (var (pattern, id) in masks)
        {
            text = pattern.Replace(text, id);
        }
        return text;
    }

    ReliabilityComment Fallback(JsonObject facts, List<string> attempts)
    {
        var lines = facts["highlights"]!.AsArray()
            .Where(h => h![Level]!.GetValue<string>() != ReliabilityReportBuilder.Info)
            .Select(h => $"**{h!["source"]!.GetValue<string>()}:** {h["text"]!.GetValue<string>()}")
            .ToList();
        lines.Add(lines.Count == 0 ? "**Vše:** Bez upozornění." : "**Ostatní:** Bez dalších upozornění.");
        return new ReliabilityComment(string.Join("\n", lines), "sablona", null, null, attempts);
    }

    JsonNode? Anonymize(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var copy = new JsonObject();
                foreach (var (key, value) in obj)
                {
                    copy[AnonymizeText(key)] = Anonymize(value);
                }
                return copy;
            case JsonArray array:
                return new JsonArray(array.Select(Anonymize).ToArray());
            case JsonValue value when value.TryGetValue<string>(out var text):
                return AnonymizeText(text);
            default:
                return node?.DeepClone();
        }
    }

    string AnonymizeText(string text)
    {
        text = Mask(text);
        foreach (var (pattern, replacement) in anonymization)
        {
            text = Regex.Replace(text, pattern, replacement, RegexOptions.None, regexTimeout);
        }
        return text;
    }

    string Cut(string? text, int length) => text is { Length: > 0 } t && t.Length > length ? t[..length] : text ?? "";

    void Put(JsonObject target, string key, JsonNode? value)
    {
        if (value is not null && !(value is JsonValue v && v.TryGetValue<string>(out var s) && s.Length == 0)
            && !(value is JsonObject o && o.Count == 0) && !(value is JsonArray a && a.Count == 0))
        {
            target[key] = value;
        }
    }

    JsonObject Problem(JsonNode issue, bool history)
    {
        var problem = new JsonObject
        {
            ["uroven"] = issue[Level]?.DeepClone(),
            ["pocet"] = issue["count"]?.DeepClone(),
            ["zprava"] = Cut(issue[Template]?.GetValue<string>(), 140),
        };
        Put(problem, "vyjimka", issue["exception_type"]?.DeepClone());
        Put(problem, "misto_v_kodu", issue["location"]?.DeepClone());
        problem["prvni"] = issue["first"]?.DeepClone();
        problem["posledni"] = issue["last"]?.DeepClone();
        problem["nova"] = issue["new"]?.DeepClone();
        Put(problem, "souvisejici_udalosti", issue["related"]?.DeepClone());
        var parameters = new JsonObject();
        if (issue["by_param"] is JsonObject byParam)
        {
            foreach (var name in keptParameters)
            {
                Put(parameters, name, byParam[name]?.DeepClone());
            }
        }
        Put(problem, "podle_parametru", parameters);
        Put(problem, "ukazka_chyby", Sample(issue));
        if (history)
        {
            Put(problem, "trend_7_dni", issue["trend_7d"]?.DeepClone());
            Put(problem, "poprve_videno", issue["first_seen_ever"]?.DeepClone());
        }
        return problem;
    }

    string Sample(JsonNode issue)
    {
        var samples = issue["samples"]?.AsArray().Where(s => s != null).ToList() ?? [];
        var exception = samples.Select(s => s!["exception_message"]?.GetValue<string>()).FirstOrDefault(m => !string.IsNullOrEmpty(m));
        if (exception != null)
        {
            return Cut(exception, 160);
        }
        var message = samples.Select(s => s!["message"]?.GetValue<string>()).FirstOrDefault(m => !string.IsNullOrEmpty(m));
        if (message == null || message == issue[Template]?.GetValue<string>())
        {
            return "";
        }
        return Cut(Regex.Replace(message, @"\s+", " ", RegexOptions.None, regexTimeout), 240);
    }

    JsonArray Topology() => new(settings.Options.Topology.Where(c => c.Id.Length > 0).Select(c =>
    {
        var component = new JsonObject { ["id"] = c.Id, ["typ"] = c.Kind };
        Put(component, "role", c.Role);
        Put(component, "zavisi_na", new JsonArray(c.DependsOn.Select(d => (JsonNode)d).ToArray()));
        Put(component, "posila", c.Sends);
        Put(component, "pres", new JsonArray(c.Via.Select(v => (JsonNode)v).ToArray()));
        return (JsonNode)component;
    }).ToArray());

    JsonObject Device(JsonNode device)
    {
        var result = new JsonObject
        {
            ["id"] = device["name"]?.DeepClone(),
            ["stav"] = device["status"]?.DeepClone(),
        };
        Put(result, "chyba_dotazu", device[ReliabilityReportBuilder.Error]?.DeepClone());
        Put(result, "dostupnost_pct", device["availability_pct"]?.DeepClone());
        Put(result, "mezery_v_datech", device["gaps"]?.DeepClone());
        Put(result, "rebooty", device["reboot_times"]?.DeepClone());
        Put(result, "reset_reason", device["reset_reasons"]?.DeepClone());
        if (device["offline_at_end"]?.GetValue<bool>() == true)
        {
            result["offline_na_konci_dne"] = true;
        }
        Put(result, "rssi_min", device["rssi_min"]?.DeepClone());
        Put(result, "souvisejici_udalosti", device["related"]?.DeepClone());
        return result;
    }

    JsonArray HourRanges(JsonNode? hourly)
    {
        var ranges = new JsonArray();
        if (hourly is not JsonArray hours)
        {
            return ranges;
        }
        var counts = hours.Select(h => h?.GetValue<int>() ?? 0).ToList();
        var hour = 0;
        while (hour < counts.Count)
        {
            if (counts[hour] == 0)
            {
                hour++;
                continue;
            }
            var from = hour;
            while (hour < counts.Count && counts[hour] > 0)
            {
                hour++;
            }
            ranges.Add($"{from:00}:00-{hour:00}:00");
        }
        return ranges;
    }

    JsonObject Endpoint(JsonNode route, bool history)
    {
        var endpoint = new JsonObject
        {
            ["endpoint"] = route["route"]?.DeepClone(),
            ["pocet"] = route["count"]?.DeepClone(),
            ["p95_ms"] = route["p95_ms"]?.DeepClone(),
        };
        if (history)
        {
            Put(endpoint, "p95_prumer_7_dni_ms", route["p95_7d_ms"]?.DeepClone());
        }
        endpoint[Http5xx] = route[Http5xx]?.DeepClone();
        return endpoint;
    }

    JsonObject Application(JsonNode app, bool history)
    {
        var requests = app["requests"];
        var top = new JsonArray(requests?["top"]?.AsArray().Take(3)
            .Select(r => (JsonNode)Endpoint(r!, history))
            .ToArray() ?? []);
        var problems = new JsonArray(app["issues"]!.AsArray()
            .Where(i => i![Level]!.GetValue<string>() == ReliabilityReportBuilder.Error || i["count"]!.GetValue<int>() >= 10)
            .Select(i => (JsonNode)Problem(i!, history))
            .ToArray());
        var application = new JsonObject
        {
            ["id"] = app["name"]?.DeepClone(),
            ["restarty"] = app["restarts"]?.DeepClone(),
        };
        Put(application, "vypadky", app["outages"]?.DeepClone());
        if (history)
        {
            Put(application, "restarty_prumer_7_dni", app["restarts_7d"]?.DeepClone());
        }
        var pozadavky = new JsonObject
        {
            ["celkem"] = requests?["total"]?.DeepClone(),
            [Http5xx] = requests?[Http5xx]?.DeepClone(),
        };
        Put(pozadavky, "http_5xx_kdy", HourRanges(requests?["http_5xx_hourly"]));
        pozadavky["nejcastejsi"] = top;
        application["pozadavky"] = pozadavky;
        application["vyjimky_runtime"] = app["exceptions"]?.DeepClone();
        if (history && app["exceptions"]?.GetValue<double>() is double exceptions && app["exceptions_7d"]?.GetValue<double>() is double average
            && exceptions > 100 && exceptions > 2 * average)
        {
            application["vyjimky_runtime_prumer_7_dni"] = average;
        }
        application["logy"] = app["logs"]?.DeepClone();
        application["problemy"] = problems;
        return application;
    }

    public JsonObject Condense(JsonObject facts)
    {
        var history = facts["history_available"]?.GetValue<bool>() == true;
        var condensed = new JsonObject();
        Put(condensed, "topologie", Topology());
        condensed["den"] = facts["day"]?.DeepClone();
        condensed["upozorneni"] = new JsonArray(facts["highlights"]!.AsArray()
            .Select(h => (JsonNode)new JsonObject { ["uroven"] = h![Level]?.DeepClone(), ["zdroj"] = h["source"]?.DeepClone(), ["text"] = h["text"]?.DeepClone() })
            .ToArray());
        condensed["aplikace"] = new JsonArray(facts["apps"]!.AsArray().Select(a => (JsonNode)Application(a!, history)).ToArray());
        Put(condensed, "zarizeni", new JsonArray(facts["devices"]?.AsArray().Select(d => (JsonNode)Device(d!)).ToArray() ?? []));
        return (JsonObject)Anonymize(condensed)!;
    }
}
