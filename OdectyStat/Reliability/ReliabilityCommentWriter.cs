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

    public ReliabilityCommentWriter(ReliabilitySettings settings)
    {
        this.settings = settings;
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(settings.Options.Comment.TimeoutSeconds) };
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
                return new ReliabilityComment(text, model, tokensIn, tokensOut, attempts);
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
        return lines > 8 ? $"prilis mnoho radku ({lines})" : null;
    }

    ReliabilityComment Fallback(JsonObject facts, List<string> attempts)
    {
        var lines = facts["highlights"]!.AsArray()
            .Where(h => h!["level"]!.GetValue<string>() != ReliabilityReportBuilder.Info)
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
            ["uroven"] = issue["level"]?.DeepClone(),
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
        var sample = issue["samples"]?.AsArray()
            .Select(s => s?["exception_message"]?.GetValue<string>())
            .FirstOrDefault(m => !string.IsNullOrEmpty(m));
        Put(problem, "ukazka_chyby", Cut(sample, 160));
        if (history)
        {
            Put(problem, "trend_7_dni", issue["trend_7d"]?.DeepClone());
            Put(problem, "poprve_videno", issue["first_seen_ever"]?.DeepClone());
        }
        return problem;
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
        endpoint["http_5xx"] = route["http_5xx"]?.DeepClone();
        return endpoint;
    }

    JsonObject Application(JsonNode app, bool history)
    {
        var requests = app["requests"];
        var top = new JsonArray(requests?["top"]?.AsArray().Take(3)
            .Select(r => (JsonNode)Endpoint(r!, history))
            .ToArray() ?? []);
        var problems = new JsonArray(app["issues"]!.AsArray()
            .Where(i => i!["level"]!.GetValue<string>() == ReliabilityReportBuilder.Error || i["count"]!.GetValue<int>() >= 10)
            .Select(i => (JsonNode)Problem(i!, history))
            .ToArray());
        var application = new JsonObject
        {
            ["aplikace"] = app["name"]?.DeepClone(),
            ["restarty"] = app["restarts"]?.DeepClone(),
        };
        if (history)
        {
            Put(application, "restarty_prumer_7_dni", app["restarts_7d"]?.DeepClone());
        }
        application["pozadavky"] = new JsonObject
        {
            ["celkem"] = requests?["total"]?.DeepClone(),
            ["http_5xx"] = requests?["http_5xx"]?.DeepClone(),
            ["nejcastejsi"] = top,
        };
        application["vyjimky_runtime"] = app["exceptions"]?.DeepClone();
        if (history)
        {
            Put(application, "vyjimky_runtime_prumer_7_dni", app["exceptions_7d"]?.DeepClone());
        }
        application["logy"] = app["logs"]?.DeepClone();
        application["problemy"] = problems;
        return application;
    }

    public JsonObject Condense(JsonObject facts)
    {
        var sources = settings.Apps.Append("server").ToHashSet();
        var history = facts["history_available"]?.GetValue<bool>() == true;
        var condensed = new JsonObject
        {
            ["den"] = facts["day"]?.DeepClone(),
            ["upozorneni"] = new JsonArray(facts["highlights"]!.AsArray()
                .Where(h => sources.Contains(h!["source"]!.GetValue<string>()))
                .Select(h => (JsonNode)new JsonObject { ["uroven"] = h!["level"]?.DeepClone(), ["zdroj"] = h["source"]?.DeepClone(), ["text"] = h["text"]?.DeepClone() })
                .ToArray()),
            ["aplikace"] = new JsonArray(facts["apps"]!.AsArray().Select(a => (JsonNode)Application(a!, history)).ToArray()),
        };
        return (JsonObject)Anonymize(condensed)!;
    }
}
