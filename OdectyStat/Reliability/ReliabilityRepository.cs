using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OdectyStat1.Business;
using OdectyStat1.DataLayer;

namespace OdectyStat1.Reliability;

public record DeviceDefinition(string Name, string Table, string Where, string Uptime, string[] Counters, string[] Maxima, string[] Minima, bool HasFirmware, int? Gauge = null);

public class DeviceStats
{
    public Dictionary<string, double> Numbers { get; } = new();
    public List<DateTimeOffset> Reboots { get; } = new();
    public List<string> ResetReasons { get; } = new();
    public List<string> FwVersions { get; } = new();
    public DateTimeOffset? LastSeen { get; set; }
    public bool OfflineAtEnd { get; set; }
}

public class ReliabilityRepository
{
    readonly DiagDbContext db;

    public ReliabilityRepository(DiagDbContext db)
    {
        this.db = db;
    }

    public bool UseHistory { get; set; } = true;

    async Task<NpgsqlConnection> Connection()
    {
        await db.Database.OpenConnectionAsync();
        return (NpgsqlConnection)db.Database.GetDbConnection();
    }

    public async Task<Dictionary<(string, string, string), double>> MetricAverages(DateOnly day)
    {
        if (!UseHistory)
        {
            return new();
        }
        var from = day.AddDays(-7);
        var averages = await db.ReliabilityDailyMetrics.AsNoTracking()
            .Where(m => m.Day >= from && m.Day < day)
            .GroupBy(m => new { m.Source, m.Metric, m.Key })
            .Select(g => new { g.Key.Source, g.Key.Metric, g.Key.Key, Value = g.Average(m => m.Value) })
            .ToListAsync();
        return averages.ToDictionary(a => (a.Source, a.Metric, a.Key), a => a.Value);
    }

    public async Task<(Dictionary<string, (DateOnly FirstSeen, Dictionary<DateOnly, int> Counts)> Issues, bool HasHistory)> IssueHistory(DateOnly day)
    {
        var result = new Dictionary<string, (DateOnly, Dictionary<DateOnly, int>)>();
        if (!UseHistory)
        {
            return (result, false);
        }
        var rows = await db.ReliabilityDailyIssues.AsNoTracking()
            .Where(i => i.Day < day)
            .Select(i => new { i.Fingerprint, i.Day, i.Count })
            .ToListAsync();
        foreach (var group in rows.GroupBy(r => r.Fingerprint))
        {
            result[group.Key] = (group.Min(r => r.Day), group.ToDictionary(r => r.Day, r => r.Count));
        }
        var limit = day.AddDays(-7);
        var hasHistory = await db.ReliabilityDailyReports.AnyAsync(r => r.Day <= limit);
        return (result, hasHistory);
    }

    public async Task<List<int>> CameraGauges(DateTimeOffset start)
    {
        var from = start.AddDays(-7).UtcDateTime;
        return await db.DeviceDiagnostics.AsNoTracking()
            .Where(d => d.Timestamp >= from)
            .Select(d => d.GaugeId)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }

    public async Task<DeviceStats> DeviceStats(DeviceDefinition d, DateTimeOffset start, DateTimeOffset end)
    {
        var counters = string.Concat(d.Counters.Select(c => $", {c}::double precision AS c_{c}"));
        var lags = string.Concat(d.Counters.Select(c => $", lag(c_{c}) OVER o AS p_{c}"));
        var deltas = string.Concat(d.Counters.Select(c => $", sum(CASE WHEN p_{c} IS NULL OR c_{c} IS NULL THEN 0 WHEN c_{c} >= p_{c} THEN c_{c} - p_{c} ELSE c_{c} END) AS {c}"));
        var maxima = string.Concat(d.Maxima.Select(c => $", max({c})::double precision AS {c}_max"));
        var minima = string.Concat(d.Minima.Select(c => $", min({c})::double precision AS {c}_min"));
        var rawMax = string.Concat(d.Maxima.Concat(d.Minima).Distinct().Select(c => $", {c}"));
        var fw = d.HasFirmware ? "fw_version::text" : "NULL::text";

        var sql = $"""
            WITH raw AS (
                SELECT timestamp AS ts, {d.Uptime}::double precision AS up, rssi::double precision AS rssi, reset_reason::text AS rr, {fw} AS fw {counters} {rawMax}
                FROM {d.Table}
                WHERE {d.Where} AND timestamp >= @prev AND timestamp < @to
            ),
            w AS (
                SELECT *, lag(ts) OVER o AS pts, lag(up) OVER o AS pup {lags}
                FROM raw
                WINDOW o AS (ORDER BY ts)
            ),
            med AS (
                SELECT percentile_cont(0.5) WITHIN GROUP (ORDER BY extract(epoch FROM ts - pts)) AS interval_s
                FROM w WHERE pts IS NOT NULL
            )
            SELECT count(*) AS rows,
                   max(ts) AS last_ts,
                   count(*) FILTER (WHERE up < pup) AS reboots,
                   coalesce(array_agg(ts ORDER BY ts) FILTER (WHERE up < pup), ARRAY[]::timestamptz[]) AS reboot_ts,
                   coalesce(array_remove(array_agg(DISTINCT rr) FILTER (WHERE up < pup), NULL), ARRAY[]::text[]) AS reasons,
                   coalesce(array_remove(array_agg(fw ORDER BY ts), NULL), ARRAY[]::text[]) AS fws,
                   min(rssi) AS rssi_min,
                   avg(rssi) AS rssi_avg,
                   (SELECT interval_s FROM med) AS interval_s,
                   coalesce(sum(extract(epoch FROM ts - greatest(pts, @from)) - (SELECT interval_s FROM med))
                       FILTER (WHERE pts IS NOT NULL AND extract(epoch FROM ts - pts) > 3 * (SELECT interval_s FROM med)), 0) AS gap_s
                   {deltas} {maxima} {minima}
            FROM w
            WHERE ts >= @from
            """;

        var stats = new DeviceStats();
        var connection = await Connection();
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("prev", start.AddDays(-1).UtcDateTime);
        cmd.Parameters.AddWithValue("from", start.UtcDateTime);
        cmd.Parameters.AddWithValue("to", end.UtcDateTime);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        var rows = reader.GetInt64(reader.GetOrdinal("rows"));
        var daySeconds = (end - start).TotalSeconds;
        stats.Numbers["messages"] = rows;
        double? interval = reader.IsDBNull(reader.GetOrdinal("interval_s")) ? null : reader.GetDouble(reader.GetOrdinal("interval_s"));
        if (rows == 0 || interval is null)
        {
            stats.Numbers["availability_pct"] = 0;
            stats.OfflineAtEnd = true;
            return stats;
        }

        var last = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("last_ts")), DateTimeKind.Utc));
        stats.LastSeen = last;
        var gap = Convert.ToDouble(reader.GetValue(reader.GetOrdinal("gap_s")), CultureInfo.InvariantCulture);
        var tail = (end - last).TotalSeconds;
        if (tail > 3 * interval.Value)
        {
            gap += tail - interval.Value;
            stats.OfflineAtEnd = true;
        }
        stats.Numbers["interval_s"] = Math.Round(interval.Value);
        stats.Numbers["availability_pct"] = Math.Round(Math.Max(0, 1 - gap / daySeconds) * 100, 2);
        stats.Numbers["reboots"] = reader.GetInt64(reader.GetOrdinal("reboots"));
        foreach (var ts in reader.GetFieldValue<DateTime[]>(reader.GetOrdinal("reboot_ts")))
        {
            stats.Reboots.Add(new DateTimeOffset(DateTime.SpecifyKind(ts, DateTimeKind.Utc)));
        }
        stats.ResetReasons.AddRange(reader.GetFieldValue<string[]>(reader.GetOrdinal("reasons")));
        foreach (var version in reader.GetFieldValue<string[]>(reader.GetOrdinal("fws")))
        {
            if (stats.FwVersions.Count == 0 || stats.FwVersions[^1] != version)
            {
                stats.FwVersions.Add(version);
            }
        }
        foreach (var column in new[] { "rssi_min", "rssi_avg" }
            .Concat(d.Counters)
            .Concat(d.Maxima.Select(c => c + "_max"))
            .Concat(d.Minima.Select(c => c + "_min")))
        {
            var ordinal = reader.GetOrdinal(column);
            if (!reader.IsDBNull(ordinal))
            {
                stats.Numbers[column] = Math.Round(Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture), 2);
            }
        }
        return stats;
    }

    public async Task<Dictionary<string, double>> CameraPipeline(int gauge, DateTimeOffset start, DateTimeOffset end)
    {
        var result = new Dictionary<string, double>();
        var connection = await Connection();
        await using (var cmd = new NpgsqlCommand("""
            SELECT count(*), avg(success::int) * 100, avg(confidence)
            FROM file_diagnostics WHERE gauge_id = @g AND timestamp >= @from AND timestamp < @to
            """, connection))
        {
            cmd.Parameters.AddWithValue("g", gauge);
            cmd.Parameters.AddWithValue("from", start.UtcDateTime);
            cmd.Parameters.AddWithValue("to", end.UtcDateTime);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            result["recognitions"] = reader.GetInt64(0);
            if (!reader.IsDBNull(1))
            {
                result["recognition_ok_pct"] = Math.Round(Convert.ToDouble(reader.GetValue(1), CultureInfo.InvariantCulture), 1);
            }
            if (!reader.IsDBNull(2))
            {
                result["recognition_confidence_avg"] = Math.Round(Convert.ToDouble(reader.GetValue(2), CultureInfo.InvariantCulture), 3);
            }
        }
        await using (var cmd = new NpgsqlCommand("""
            SELECT count(*), avg(success::int) * 100, avg(duration_ms), percentile_cont(0.95) WITHIN GROUP (ORDER BY duration_ms), avg(try_count)
            FROM transfer_diagnostics WHERE gauge_id = @g AND timestamp >= @from AND timestamp < @to
            """, connection))
        {
            cmd.Parameters.AddWithValue("g", gauge);
            cmd.Parameters.AddWithValue("from", start.UtcDateTime);
            cmd.Parameters.AddWithValue("to", end.UtcDateTime);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            result["transfers"] = reader.GetInt64(0);
            string[] names = ["", "transfer_ok_pct", "transfer_avg_ms", "transfer_p95_ms", "transfer_tries_avg"];
            for (var i = 1; i < names.Length; i++)
            {
                if (!reader.IsDBNull(i))
                {
                    result[names[i]] = Math.Round(Convert.ToDouble(reader.GetValue(i), CultureInfo.InvariantCulture), 2);
                }
            }
        }
        return result;
    }

    public async Task Save(DateOnly day, ReliabilityReportBuilder builder)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.ReliabilityDailyMetrics.Where(m => m.Day == day).ExecuteDeleteAsync();
        await db.ReliabilityDailyIssues.Where(i => i.Day == day).ExecuteDeleteAsync();

        db.ReliabilityDailyMetrics.AddRange(builder.Metrics
            .GroupBy(m => (m.Source, m.Name, m.Key))
            .Select(g => new ReliabilityDailyMetric { Day = day, Source = g.Key.Source, Metric = g.Key.Name, Key = g.Key.Key, Value = g.Last().Value }));

        db.ReliabilityDailyIssues.AddRange(builder.Issues.Select(issue => new ReliabilityDailyIssue
        {
            Day = day,
            Fingerprint = issue.Fingerprint,
            Source = issue.Source,
            Level = issue.Level,
            Template = issue.Template,
            ExceptionType = issue.ExceptionType is { Length: > 0 } e ? e : null,
            Location = issue.Location,
            Count = issue.Count,
            FirstTs = issue.First?.UtcDateTime,
            LastTs = issue.Last?.UtcDateTime,
            Hourly = issue.Hourly,
            Details = issue.Details.ToJsonString(),
        }));

        var report = await db.ReliabilityDailyReports.FindAsync(day);
        if (report == null)
        {
            report = new ReliabilityDailyReport { Day = day, Status = builder.Status, Facts = builder.Facts!.ToJsonString() };
            db.ReliabilityDailyReports.Add(report);
        }
        report.Status = builder.Status;
        report.Facts = builder.Facts!.ToJsonString();
        report.CreatedAt = DateTime.UtcNow;
        if (builder.Comment != null)
        {
            report.Comment = builder.Comment.Text;
            report.CommentModel = builder.Comment.Model;
            report.CommentTokensIn = builder.Comment.TokensIn;
            report.CommentTokensOut = builder.Comment.TokensOut;
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
