using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Npgsql;

namespace OdectyStat1.Reliability;

public record ReliabilityMetric(string Source, string Name, string Key, double Value);
public record ReliabilityHighlight(string Level, string Source, string Text);
record ReliabilityEvent(string Source, string Kind, DateTimeOffset Time);

public class ReliabilityIssue
{
    public required string Source { get; init; }
    public required string Level { get; init; }
    public required string Template { get; init; }
    public required string ExceptionType { get; init; }
    public required string Fingerprint { get; init; }
    public int[] Hourly { get; } = new int[24];
    public int Count => Hourly.Sum();
    public DateTimeOffset? First { get; set; }
    public DateTimeOffset? Last { get; set; }
    public JsonObject Details { get; } = new();
    public string? Location { get; set; }
}

public class ReliabilityReportBuilder
{
    public const string Fail = "fail";
    public const string Warn = "warn";
    public const string New = "new";
    public const string Info = "info";
    public const string Error = "error";
    public const string Ok = "ok";
    const string Restart = "restart";
    const string Reboot = "reboot";
    const string Requests = "requests";
    const string Http5xx = "http_5xx";
    const string CountKey = "count";
    const string Route = "route";
    const string Top = "top";
    const string Availability = "availability_pct";
    const string Restarts = "restarts";
    const string Exceptions = "exceptions";
    const string Values = "values";
    const string Value = "value";
    const string TimeFormat = "HH:mm:ss";
    const string DayFormat = "yyyy-MM-dd";
    const string TemplateLabel = "label_format tmpl=`{{ or ._OriginalFormat_ .message_template_text }}`";

    readonly ReliabilitySettings settings;
    readonly GrafanaClient grafana;
    readonly ReliabilityRepository repository;
    readonly DateOnly day;
    readonly DateTimeOffset start;
    readonly DateTimeOffset end;
    readonly long seconds;
    readonly string jobs;
    readonly string range;
    readonly Dictionary<string, JsonObject> apps = new();
    readonly List<JsonObject> devices = new();
    readonly List<ReliabilityEvent> events = new();
    readonly Dictionary<string, int[]> http5xxHourly = new();
    readonly TimeSpan regexTimeout = TimeSpan.FromSeconds(1);
    Dictionary<(string Source, string Name, string Key), double> history = new();
    Dictionary<string, (DateOnly FirstSeen, Dictionary<DateOnly, int> Counts)> issueHistory = new();
    bool hasHistory;

    public List<ReliabilityMetric> Metrics { get; } = new();
    public List<ReliabilityIssue> Issues { get; } = new();
    public List<ReliabilityHighlight> Highlights { get; } = new();
    public JsonObject? Facts { get; private set; }
    public string Status { get; private set; } = Ok;
    public ReliabilityComment? Comment { get; set; }

    readonly HashSet<string> metaLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "service_name", "service_instance_id", "detected_level", "observed_timestamp", "severity_number", "severity_text",
        "scope_name", "telemetry_sdk_language", "telemetry_sdk_name", "telemetry_sdk_version", "_OriginalFormat_",
        "message_template_text", "exception_message", "exception_stacktrace", "exception_type", "trace_id", "span_id",
        "tmpl", "RequestId", "ConnectionId", "EventId_Id", "EventId_Name", "flags", "trace_flags", "SpanId", "TraceId",
        "ParentId", "newline", "level", "__error__", "__error_details__", "host_name", "service_namespace", "ActionId",
    };

    public ReliabilityReportBuilder(ReliabilitySettings settings, GrafanaClient grafana, ReliabilityRepository repository, DateOnly day)
    {
        this.settings = settings;
        this.grafana = grafana;
        this.repository = repository;
        this.day = day;
        start = LocalMidnight(day);
        end = LocalMidnight(day.AddDays(1));
        seconds = (long)(end - start).TotalSeconds;
        jobs = string.Join("|", settings.Apps.Select(Regex.Escape));
        range = $"{seconds}s";
    }

    DateTimeOffset LocalMidnight(DateOnly d)
    {
        var local = d.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, settings.Zone.GetUtcOffset(local));
    }

    string Local(DateTimeOffset t, string format = "HH:mm") => TimeZoneInfo.ConvertTime(t, settings.Zone).ToString(format, CultureInfo.InvariantCulture);

    double Val(JsonNode? sample) => double.Parse(sample![1]!.GetValue<string>(), CultureInfo.InvariantCulture);

    DateTimeOffset Ts(JsonNode? sample) => DateTimeOffset.FromUnixTimeMilliseconds((long)(sample![0]!.GetValue<double>() * 1000));

    DateTimeOffset LokiTs(JsonNode? sample) => DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sample![0]!.GetValue<string>(), CultureInfo.InvariantCulture) / 1_000_000);

    string Label(JsonNode? series, string name) => series?["metric"]?[name]?.GetValue<string>() ?? "";

    string RouteName(JsonNode? series) => $"{Label(series, "http_request_method")} {(Label(series, "http_route") is { Length: > 0 } r ? r : "(bez route)")}";

    int HourIndex(DateTimeOffset t) => Math.Clamp((int)Math.Round((t - start).TotalHours) - 1, 0, 23);

    void Add(string source, string name, double value, string key = "") => Metrics.Add(new ReliabilityMetric(source, name, key, value));

    double? Avg7(string source, string name, string key = "") => history.TryGetValue((source, name, key), out var v) ? v : null;

    JsonObject App(string name)
    {
        if (!apps.TryGetValue(name, out var app))
        {
            app = new JsonObject { ["name"] = name };
            apps[name] = app;
        }
        return app;
    }

    void Highlight(string level, string source, string text) => Highlights.Add(new ReliabilityHighlight(level, source, text));

    public async Task<JsonObject> Build()
    {
        history = await repository.MetricAverages(day);
        (issueHistory, hasHistory) = await repository.IssueHistory(day);
        foreach (var app in settings.Apps)
        {
            App(app);
        }

        await CollectRequests();
        await CollectRuntime();
        await CollectAvailability();
        await CollectLogs();
        await CollectDevices();
        BuildRelated();
        BuildAppHighlights();
        BuildIssueHighlights();
        BuildDeviceHighlights();
        BuildClusterHighlights();
        var order = new Dictionary<string, int> { [Fail] = 0, [Warn] = 1, [New] = 2, [Info] = 3 };
        Highlights.Sort((a, b) => order[a.Level].CompareTo(order[b.Level]));
        Status = WorstLevel(Highlights);
        return Facts = Assemble();
    }

    string WorstLevel(IEnumerable<ReliabilityHighlight> highlights)
    {
        var levels = highlights.Select(h => h.Level).ToHashSet();
        if (levels.Contains(Fail))
        {
            return Fail;
        }
        return levels.Contains(Warn) || levels.Contains(New) ? Warn : Ok;
    }

    async Task CollectRequests()
    {
        var routes = await RouteCounts();
        await RouteLatencies(routes);

        var hourly = await grafana.PromRange($"sum by (job) (increase(http_server_request_duration_seconds_count{{job=~\"{jobs}\", http_response_status_code=~\"5..\"}}[1h]))", start.AddHours(1), end, 3600);
        foreach (var s in hourly)
        {
            var arr = new int[24];
            foreach (var v in s![Values]!.AsArray())
            {
                arr[HourIndex(Ts(v))] += (int)Math.Round(Val(v));
            }
            http5xxHourly[Label(s, "job")] = arr;
        }

        foreach (var app in settings.Apps)
        {
            var appRoutes = routes.Where(r => r.Key.App == app).Select(r => r.Value).ToList();
            var total = appRoutes.Sum(r => r[CountKey]!.GetValue<double>());
            var c4 = appRoutes.Sum(r => r["http_4xx"]!.GetValue<double>());
            var c5 = appRoutes.Sum(r => r[Http5xx]!.GetValue<double>());
            Add(app, Requests, total);
            Add(app, "http_4xx", c4);
            Add(app, Http5xx, c5);
            var top = new JsonArray();
            foreach (var stats in appRoutes.OrderByDescending(r => r[CountKey]!.GetValue<double>()))
            {
                var route = stats[Route]!.GetValue<string>();
                Add(app, "route_requests", stats[CountKey]!.GetValue<double>(), route);
                if (stats["p95_ms"] is JsonNode p95)
                {
                    Add(app, "route_p95_ms", p95.GetValue<double>(), route);
                    if (Avg7(app, "route_p95_ms", route) is double avg)
                    {
                        stats["p95_7d_ms"] = Math.Round(avg, 1);
                    }
                }
                if (top.Count < 10)
                {
                    top.Add(stats.DeepClone());
                }
            }
            App(app)[Requests] = new JsonObject
            {
                ["total"] = total,
                ["http_4xx"] = c4,
                [Http5xx] = c5,
                ["http_5xx_hourly"] = http5xxHourly.TryGetValue(app, out var h) ? new JsonArray(h.Select(x => (JsonNode)x).ToArray()) : null,
                [Top] = top,
            };
        }
    }

    async Task<Dictionary<(string App, string Route), JsonObject>> RouteCounts()
    {
        var routes = new Dictionary<(string App, string Route), JsonObject>();
        var counts = await grafana.PromInstant($"sum by (job, http_route, http_request_method, http_response_status_code) (increase(http_server_request_duration_seconds_count{{job=~\"{jobs}\"}}[{range}]))", end);
        foreach (var s in counts)
        {
            var count = Math.Round(Val(s![Value]));
            if (count < 1)
            {
                continue;
            }
            var app = Label(s, "job");
            var route = RouteName(s);
            var status = Label(s, "http_response_status_code");
            if (!routes.TryGetValue((app, route), out var stats))
            {
                stats = new JsonObject { [Route] = route, [CountKey] = 0.0, ["http_4xx"] = 0.0, [Http5xx] = 0.0 };
                routes[(app, route)] = stats;
            }
            stats[CountKey] = stats[CountKey]!.GetValue<double>() + count;
            if (status.StartsWith('4'))
            {
                stats["http_4xx"] = stats["http_4xx"]!.GetValue<double>() + count;
            }
            if (status.StartsWith('5'))
            {
                stats[Http5xx] = stats[Http5xx]!.GetValue<double>() + count;
            }
        }
        return routes;
    }

    async Task RouteLatencies(Dictionary<(string App, string Route), JsonObject> routes)
    {
        foreach (var (quantile, name) in new[] { (0.5, "p50_ms"), (0.95, "p95_ms"), (0.99, "p99_ms") })
        {
            var result = await grafana.PromInstant($"histogram_quantile({quantile.ToString(CultureInfo.InvariantCulture)}, sum by (job, http_route, http_request_method, le) (increase(http_server_request_duration_seconds_bucket{{job=~\"{jobs}\"}}[{range}])))", end);
            foreach (var s in result)
            {
                var value = Val(s![Value]);
                if (double.IsNaN(value) || !routes.TryGetValue((Label(s, "job"), RouteName(s)), out var stats))
                {
                    continue;
                }
                stats[name] = Math.Round(value * 1000, 1);
            }
        }
    }

    async Task CollectRuntime()
    {
        var exceptions = await grafana.PromInstant($"sum by (job) (increase(dotnet_exceptions_total{{job=~\"{jobs}\"}}[{range}]))", end);
        foreach (var s in exceptions)
        {
            var app = Label(s, "job");
            var value = Math.Round(Val(s![Value]));
            Add(app, Exceptions, value);
            App(app)[Exceptions] = value;
            if (Avg7(app, Exceptions) is double avg)
            {
                App(app)["exceptions_7d"] = Math.Round(avg);
            }
        }
        var memory = await grafana.PromInstant($"max by (job) (max_over_time(dotnet_process_memory_working_set_bytes{{job=~\"{jobs}\"}}[{range}]))", end);
        foreach (var s in memory)
        {
            var app = Label(s, "job");
            var mb = Math.Round(Val(s![Value]) / 1024 / 1024);
            Add(app, "memory_max_mb", mb);
            App(app)["memory_max_mb"] = mb;
        }
    }

    async Task CollectAvailability()
    {
        var buckets = (int)(seconds / 300);
        var series = await grafana.PromRange($"count by (job, instance) (count_over_time(target_info{{job=~\"{jobs}\"}}[5m]))", start.AddMinutes(5), end, 300);
        foreach (var app in settings.Apps)
        {
            var covered = new HashSet<long>();
            var restartTimes = new List<DateTimeOffset>();
            foreach (var s in series.Where(s => Label(s, "job") == app))
            {
                var values = s![Values]!.AsArray();
                foreach (var v in values)
                {
                    covered.Add(Ts(v).ToUnixTimeSeconds());
                }
                var first = Ts(values[0]);
                if (first > start.AddMinutes(10))
                {
                    restartTimes.Add(first.AddMinutes(-5));
                }
            }
            restartTimes.Sort();
            events.AddRange(restartTimes.Select(r => new ReliabilityEvent(app, Restart, r)));
            var restarts = new JsonArray(restartTimes.Select(r => (JsonNode)Local(r)).ToArray());
            var availability = Math.Round(100.0 * Math.Min(covered.Count, buckets) / buckets, 2);
            Add(app, Availability, availability);
            Add(app, Restarts, restarts.Count);
            if (Avg7(app, Restarts) is double restartsAvg)
            {
                App(app)["restarts_7d"] = Math.Round(restartsAvg, 1);
            }
            App(app)[Availability] = availability;
            App(app)[Restarts] = restarts;
        }
    }

    async Task CollectLogs()
    {
        var levels = await grafana.LokiInstant($"sum by (service_name, detected_level) (count_over_time({{service_name=~\"{jobs}\"}}[{range}]))", end);
        foreach (var app in settings.Apps)
        {
            App(app)["logs"] = new JsonObject { [Error] = 0, [Warn] = 0, [Info] = 0 };
        }
        foreach (var s in levels)
        {
            var app = Label(s, "service_name");
            var level = Label(s, "detected_level");
            var count = (int)Math.Round(Val(s![Value]));
            if (App(app)["logs"] is JsonObject logs)
            {
                logs[level] = count;
            }
            Add(app, $"log_{level}", count);
        }

        var groups = await grafana.LokiRange(
            $"sum by (service_name, detected_level, tmpl, exception_type) (count_over_time({{service_name=~\"{jobs}\"}} | detected_level=~\"error|warn\" | {TemplateLabel} [1h]))",
            start.AddHours(1), end, 3600);
        foreach (var s in groups)
        {
            var source = Label(s, "service_name");
            var level = Label(s, "detected_level");
            var template = Label(s, "tmpl");
            var exception = Label(s, "exception_type");
            var issue = new ReliabilityIssue
            {
                Source = source,
                Level = level,
                Template = template,
                ExceptionType = exception,
                Fingerprint = Fingerprint(source, level, template, exception),
            };
            foreach (var v in s![Values]!.AsArray())
            {
                issue.Hourly[HourIndex(Ts(v))] += (int)Math.Round(Val(v));
            }
            if (issue.Count > 0)
            {
                Issues.Add(issue);
            }
        }

        foreach (var issue in Issues)
        {
            await CollectIssueDetails(issue);
            Add(issue.Source, "issue_count", issue.Count, issue.Fingerprint);
        }
    }

    string Fingerprint(params string[] parts) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(string.Join("|", parts))))[..12].ToLowerInvariant();

    string IssueQuery(ReliabilityIssue issue)
    {
        var query = $"{{service_name=\"{issue.Source}\"}} | detected_level=\"{issue.Level}\" | {TemplateLabel}";
        if (!issue.Template.Contains('`'))
        {
            query += $" | tmpl=`{issue.Template}`";
        }
        if (!issue.ExceptionType.Contains('`'))
        {
            query += $" | exception_type=`{issue.ExceptionType}`";
        }
        return query;
    }

    async Task CollectIssueDetails(ReliabilityIssue issue)
    {
        var query = IssueQuery(issue);
        var newest = await grafana.LokiLogs(query, start, end, 100, "backward");
        var oldest = await grafana.LokiLogs(query, start, end, 1, "forward");

        var entries = new List<(DateTimeOffset Ts, string Line, JsonObject Labels)>();
        foreach (var stream in newest)
        {
            var labels = stream!["stream"]!.AsObject();
            entries.AddRange(stream[Values]!.AsArray().Select(v => (LokiTs(v), v![1]!.GetValue<string>(), labels)));
        }
        entries = entries.OrderByDescending(e => e.Ts).ToList();
        var firstEntry = oldest.SelectMany(s => s![Values]!.AsArray()).FirstOrDefault();
        issue.First = firstEntry != null ? LokiTs(firstEntry) : entries.LastOrDefault().Ts;
        issue.Last = entries.Count > 0 ? entries[0].Ts : issue.First;

        var locations = new Dictionary<string, int>();
        var scopes = new Dictionary<string, int>();
        var parameters = new Dictionary<string, Dictionary<string, int>>();
        var samples = new JsonArray();
        var seen = new HashSet<string>();
        foreach (var (ts, line, labels) in entries)
        {
            if (Location(labels["exception_stacktrace"]?.GetValue<string>()) is string location)
            {
                locations[location] = locations.GetValueOrDefault(location) + 1;
            }
            if (labels["scope_name"]?.GetValue<string>() is string scope)
            {
                scopes[scope] = scopes.GetValueOrDefault(scope) + 1;
            }
            CountParameters(labels, parameters);
            var exceptionMessage = labels["exception_message"]?.GetValue<string>();
            var key = Regex.Replace(line + "|" + exceptionMessage, @"\d+([.,]\d+)?", "#", RegexOptions.None, regexTimeout);
            if (samples.Count < 3 && seen.Add(key))
            {
                samples.Add(new JsonObject
                {
                    ["ts"] = Local(ts, "yyyy-MM-dd HH:mm:ss"),
                    ["message"] = line.Length > 500 ? line[..500] : line,
                    ["exception_message"] = exceptionMessage is { Length: > 300 } m ? m[..300] : exceptionMessage,
                    ["request_path"] = labels["RequestPath"]?.GetValue<string>(),
                    ["trace_id"] = labels["trace_id"]?.GetValue<string>(),
                });
            }
        }

        issue.Location = locations.OrderByDescending(l => l.Value).Select(l => l.Key).FirstOrDefault();
        var (byParam, varied) = SummarizeParameters(parameters);
        issue.Details["scope"] = scopes.OrderByDescending(s => s.Value).Select(s => s.Key).FirstOrDefault();
        issue.Details["locations"] = new JsonArray(locations.OrderByDescending(l => l.Value).Take(3)
            .Select(l => (JsonNode)new JsonObject { ["location"] = l.Key, ["samples"] = l.Value }).ToArray());
        issue.Details["sample_size"] = entries.Count;
        issue.Details["by_param"] = byParam;
        issue.Details["varied_params"] = varied;
        issue.Details["samples"] = samples;
        issue.Details["explore_url"] = grafana.ExploreUrl(query, start, end);
    }

    void CountParameters(JsonObject labels, Dictionary<string, Dictionary<string, int>> parameters)
    {
        foreach (var (name, value) in labels)
        {
            if (metaLabels.Contains(name) || value == null)
            {
                continue;
            }
            var text = value.GetValue<string>();
            text = text.Length > 120 ? text[..120] : text;
            if (!parameters.TryGetValue(name, out var values))
            {
                values = new Dictionary<string, int>();
                parameters[name] = values;
            }
            values[text] = values.GetValueOrDefault(text) + 1;
        }
    }

    (JsonObject ByParam, JsonArray Varied) SummarizeParameters(Dictionary<string, Dictionary<string, int>> parameters)
    {
        var byParam = new JsonObject();
        var varied = new JsonArray();
        foreach (var (name, values) in parameters.OrderBy(p => p.Key))
        {
            if (values.Count > 20 || (values.Count > 3 && values.Keys.All(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _))))
            {
                varied.Add(name);
                continue;
            }
            var top = new JsonObject();
            foreach (var (value, count) in values.OrderByDescending(v => v.Value).Take(5))
            {
                top[value] = count;
            }
            byParam[name] = top;
        }
        return (byParam, varied);
    }

    string? Location(string? stacktrace)
    {
        if (string.IsNullOrEmpty(stacktrace))
        {
            return null;
        }
        string? fallback = null;
        foreach (Match m in Regex.Matches(stacktrace, @"^\s*at (?<frame>[^\(\r\n]+)\([^\)\r\n]*\)(?: in (?<file>[^\r\n]+):line (?<line>\d+))?", RegexOptions.Multiline, regexTimeout))
        {
            var frame = m.Groups["frame"].Value.Trim();
            var formatted = m.Groups["file"].Success ? $"{frame} ({Path.GetFileName(m.Groups["file"].Value)}:{m.Groups["line"].Value})" : frame;
            fallback ??= formatted;
            if (settings.OwnNamespaces.Any(ns => frame.StartsWith(ns + ".", StringComparison.Ordinal)))
            {
                return formatted;
            }
        }
        return fallback;
    }

    async Task<List<DeviceDefinition>> DeviceDefinitions()
    {
        var definitions = new List<DeviceDefinition>
        {
            new("heater", "heater_diagnostics", "true", "uptime_minutes", ["wifi_reconnects", "mqtt_reconnects", "sensor_errors"], ["loop_max_ms"], [], false),
            new("lssensor", "ls_sensor_diagnostics", "true", "uptime_minutes", ["wifi_reconnects", "mqtt_fail_count", "ota_fail_count", "sampler_overruns"], ["loop_max_ms", "sampler_max_us"], ["sampler_stack_words"], true),
            new("garage", "garage_diagnostics", "true", "uptime_minutes", ["wifi_reconnects", "mqtt_reconnects", "sensor_errors", "ota_fail_count", "door_cycles"], ["loop_max_ms"], [], true),
            new("fve", "fve_diagnostics", "true", "uptime_minutes", ["wifi_reconnects", "mqtt_fail_count", "ota_fail_count", "bel_frame_errors", "load_dropouts"], ["loop_max_ms", "fan_mismatch_slots"], ["min_free_heap_kb"], true),
        };
        foreach (var gauge in await repository.CameraGauges(start))
        {
            definitions.Add(new($"camera-gauge{gauge}", "device_diagnostics", $"gauge_id = {gauge}", "uptime_seconds",
                ["wifi_reconnects", "send_failures", "tls_errors", "ota_failures", "camera_errors", "capture_count"], ["loop_max_ms"], ["min_free_heap_kb"], true, gauge));
        }
        return definitions;
    }

    async Task CollectDevices()
    {
        foreach (var definition in await DeviceDefinitions())
        {
            var name = definition.Name;
            DeviceStats device;
            try
            {
                device = await repository.DeviceStats(definition, start, end);
            }
            catch (PostgresException ex)
            {
                devices.Add(new JsonObject { ["name"] = name, ["table"] = definition.Table, [Error] = ex.MessageText });
                Highlight(Warn, name, $"Chyba dotazu na {definition.Table}: {ex.MessageText}");
                continue;
            }
            var json = new JsonObject { ["name"] = name, ["table"] = definition.Table };
            foreach (var (metric, value) in device.Numbers)
            {
                Add(name, metric, value);
                json[metric] = Math.Round(value, 2);
            }
            events.AddRange(device.Reboots.Select(r => new ReliabilityEvent(name, Reboot, r)));
            json["last_seen"] = device.LastSeen is DateTimeOffset seen ? Local(seen, "yyyy-MM-dd HH:mm") : null;
            json["reboot_times"] = new JsonArray(device.Reboots.Select(r => (JsonNode)Local(r)).ToArray());
            json["reset_reasons"] = new JsonArray(device.ResetReasons.Select(r => (JsonNode)r).ToArray());
            json["fw_versions"] = new JsonArray(device.FwVersions.Select(r => (JsonNode)r).ToArray());
            json["offline_at_end"] = device.OfflineAtEnd;

            if (definition.Gauge is int g)
            {
                foreach (var (metric, value) in await repository.CameraPipeline(g, start, end))
                {
                    Add(name, metric, value);
                    json[metric] = Math.Round(value, 2);
                }
            }
            devices.Add(json);
        }
    }

    void BuildRelated()
    {
        foreach (var issue in Issues)
        {
            var related = new JsonArray();
            var hours = Enumerable.Range(0, 24).Where(h => issue.Hourly[h] > 0).ToHashSet();
            foreach (var e in events.Where(e => e.Source != issue.Source || e.Kind == Restart).OrderBy(e => e.Time))
            {
                var h = (int)Math.Floor((e.Time - start).TotalHours);
                if (hours.Contains(h) || hours.Contains(h - 1) || hours.Contains(h + 1))
                {
                    related.Add($"{e.Source}: {e.Kind} {Local(e.Time)}");
                }
            }
            foreach (var (app, hourly) in http5xxHourly)
            {
                var overlap = hours.Sum(h => hourly[h]);
                if (overlap > 0)
                {
                    related.Add($"{app}: {overlap}x HTTP 5xx ve stejnych hodinach");
                }
            }
            issue.Details["related"] = related;
        }
    }

    void AvailabilityHighlight(string source, double availability)
    {
        if (availability < 95)
        {
            Highlight(Fail, source, $"Dostupnost {availability} %");
        }
        else if (availability < 99)
        {
            Highlight(Warn, source, $"Dostupnost {availability} %");
        }
    }

    void BuildAppHighlights()
    {
        foreach (var app in settings.Apps)
        {
            var json = App(app);
            AvailabilityHighlight(app, json[Availability]?.GetValue<double>() ?? 0);
            if (json[Restarts] is JsonArray restarts && restarts.Count > 0)
            {
                Highlight(Warn, app, $"{restarts.Count}x restart ({string.Join(", ", restarts.Select(r => r!.GetValue<string>()))})");
            }
            var requests = json[Requests]!.AsObject();
            var c5 = requests[Http5xx]!.GetValue<double>();
            if (c5 > 0)
            {
                var worst = requests[Top]!.AsArray().OrderByDescending(r => r![Http5xx]!.GetValue<double>()).FirstOrDefault();
                Highlight(c5 >= 10 ? Warn : Info, app, $"{c5}x HTTP 5xx, nejvic {worst?[Route]} ({worst?[Http5xx]}x)");
            }
            foreach (var route in requests[Top]!.AsArray())
            {
                if (route!["p95_ms"]?.GetValue<double>() is double p95 && route["p95_7d_ms"]?.GetValue<double>() is double avg
                    && route[CountKey]!.GetValue<double>() >= 20 && p95 > 200 && p95 > 2 * avg)
                {
                    Highlight(Warn, app, $"{route[Route]}: p95 {p95} ms (7 dni prumer {avg} ms)");
                }
            }
            if (json[Exceptions]?.GetValue<double>() is double exceptions && json["exceptions_7d"]?.GetValue<double>() is double exAvg
                && exceptions > 100 && exceptions > 2 * exAvg)
            {
                Highlight(Info, app, $"Vyjimek {exceptions} (7 dni prumer {exAvg})");
            }
        }
    }

    void BuildIssueHighlights()
    {
        foreach (var issue in Issues)
        {
            var known = issueHistory.TryGetValue(issue.Fingerprint, out var hist);
            var counts = Enumerable.Range(1, 7).Select(d => known && hist.Counts.TryGetValue(day.AddDays(-d), out var c) ? c : 0).Reverse().ToArray();
            var avg = counts.Average();
            var isNew = hasHistory && !known;
            issue.Details["new"] = isNew;
            issue.Details["first_seen_ever"] = known ? hist.FirstSeen.ToString(DayFormat, CultureInfo.InvariantCulture) : day.ToString(DayFormat, CultureInfo.InvariantCulture);
            issue.Details["trend_7d"] = new JsonArray(counts.Select(c => (JsonNode)c).ToArray());
            var what = issue.ExceptionType is { Length: > 0 } ex ? $"{ex}: {Shorten(issue.Template)}" : Shorten(issue.Template);
            if (isNew)
            {
                Highlight(issue.Level == Error ? Warn : New, issue.Source, $"Novy {issue.Level}: {what} ({issue.Count}x)");
            }
            else if (hasHistory && issue.Count >= 10 && issue.Count > 3 * Math.Max(avg, 1))
            {
                Highlight(Warn, issue.Source, $"Roste {issue.Level}: {what} ({issue.Count}x, 7 dni prumer {Math.Round(avg, 1)})");
            }
        }
    }

    void BuildDeviceHighlights()
    {
        foreach (var device in devices)
        {
            var name = device["name"]!.GetValue<string>();
            if (device["offline_at_end"]?.GetValue<bool>() == true)
            {
                Highlight(Fail, name, $"Offline, naposledy {device["last_seen"]}");
            }
            if (device[Availability]?.GetValue<double>() is double availability)
            {
                AvailabilityHighlight(name, availability);
            }
            if (device["reboots"]?.GetValue<double>() is double reboots && reboots > 0)
            {
                Highlight(Warn, name, $"{reboots}x reboot ({string.Join(", ", device["reboot_times"]!.AsArray().Select(r => r!.GetValue<string>()))}), reset_reason {string.Join(", ", device["reset_reasons"]!.AsArray().Select(r => r!.GetValue<string>()))}");
            }
            if (device["fw_versions"] is JsonArray fw && fw.Count > 1)
            {
                Highlight(Info, name, $"Zmena firmwaru: {string.Join(" -> ", fw.Select(f => f!.GetValue<string>()))}");
            }
            if (device["rssi_min"]?.GetValue<double>() is double rssi && rssi < -80)
            {
                Highlight(Warn, name, $"Slaby signal, RSSI min {rssi} dBm");
            }
            if (device["recognition_ok_pct"]?.GetValue<double>() is double ok && Avg7(name, "recognition_ok_pct") is double okAvg && ok < okAvg - 10)
            {
                Highlight(Warn, name, $"Uspesnost rozpoznani {ok} % (7 dni prumer {Math.Round(okAvg, 1)} %)");
            }
        }
    }

    void BuildClusterHighlights()
    {
        foreach (var (kind, source, cause) in new[] { (Reboot, "iot", "pravdepodobne napajeni nebo sit"), (Restart, "server", "pravdepodobne restart serveru") })
        {
            var simultaneous = events.Where(e => e.Kind == kind).OrderBy(e => e.Time).ToList();
            var i = 0;
            while (i < simultaneous.Count)
            {
                var anchor = simultaneous[i].Time;
                var window = simultaneous.Skip(i).TakeWhile(e => e.Time - anchor <= TimeSpan.FromMinutes(10)).ToList();
                var cluster = window.Select(e => e.Source).Distinct().ToList();
                if (cluster.Count >= 2)
                {
                    Highlight(Warn, source, $"Soucasny {kind} {string.Join(", ", cluster)} kolem {Local(anchor)}: {cause}");
                    i += window.Count;
                }
                else
                {
                    i++;
                }
            }
        }
    }

    string Shorten(string text) => text.Length > 90 ? text[..90] + "..." : text;

    string ComponentStatus(string source) => WorstLevel(Highlights.Where(h => h.Source == source));

    JsonObject IssueJson(ReliabilityIssue issue)
    {
        var json = new JsonObject
        {
            ["fingerprint"] = issue.Fingerprint,
            ["level"] = issue.Level,
            ["template"] = issue.Template,
            ["exception_type"] = issue.ExceptionType is { Length: > 0 } e ? e : null,
            ["location"] = issue.Location,
            [CountKey] = issue.Count,
            ["first"] = issue.First is DateTimeOffset f ? Local(f, TimeFormat) : null,
            ["last"] = issue.Last is DateTimeOffset l ? Local(l, TimeFormat) : null,
            ["hourly"] = new JsonArray(issue.Hourly.Select(h => (JsonNode)h).ToArray()),
        };
        foreach (var (key, value) in issue.Details)
        {
            json[key] = value?.DeepClone();
        }
        return json;
    }

    JsonObject Assemble()
    {
        var appArray = new JsonArray();
        foreach (var app in settings.Apps)
        {
            var json = App(app);
            json["status"] = ComponentStatus(app);
            json["issues"] = new JsonArray(Issues.Where(i => i.Source == app)
                .OrderBy(i => i.Level == Error ? 0 : 1).ThenByDescending(i => i.Count)
                .Select(i => (JsonNode)IssueJson(i)).ToArray());
            appArray.Add(json.DeepClone());
        }
        var deviceArray = new JsonArray();
        foreach (var device in devices)
        {
            device["status"] = ComponentStatus(device["name"]!.GetValue<string>());
            deviceArray.Add(device.DeepClone());
        }
        return new JsonObject
        {
            ["day"] = day.ToString(DayFormat, CultureInfo.InvariantCulture),
            ["generated_at"] = Local(DateTimeOffset.Now, "yyyy-MM-dd HH:mm:ss"),
            ["status"] = Status,
            ["history_available"] = hasHistory,
            ["highlights"] = new JsonArray(Highlights.Select(h => (JsonNode)new JsonObject { ["level"] = h.Level, ["source"] = h.Source, ["text"] = h.Text }).ToArray()),
            ["apps"] = appArray,
            ["devices"] = deviceArray,
        };
    }
}
