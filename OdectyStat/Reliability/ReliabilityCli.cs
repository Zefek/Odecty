using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using OdectyStat1.DataLayer;

namespace OdectyStat1.Reliability;

public class ReliabilityCli
{
    public async Task<int> Run(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        DateOnly? day = null;
        var backfill = 0;
        var dryRun = false;
        var comments = true;
        var condensed = false;
        string? output = null;
        string? factsFile = null;
        var queue = new Queue<string>(args);
        while (queue.TryDequeue(out var arg))
        {
            switch (arg)
            {
                case "--day":
                    day = DateOnly.ParseExact(queue.Dequeue(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    break;
                case "--backfill":
                    backfill = int.Parse(queue.Dequeue(), CultureInfo.InvariantCulture);
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--out":
                    output = queue.Dequeue();
                    break;
                case "--no-comment":
                    comments = false;
                    break;
                case "--facts":
                    factsFile = queue.Dequeue();
                    break;
                case "--condensed":
                    condensed = true;
                    break;
                default:
                    await Console.Error.WriteLineAsync($"Neznamy parametr {arg}. Pouziti: reliability [--day yyyy-MM-dd] [--backfill N] [--dry-run] [--out soubor.json] [--no-comment] [--facts fakta.json [--condensed]]");
                    return 2;
            }
        }

        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var options = config.GetSection("Reliability").Get<ReliabilityOptions>() ?? new ReliabilityOptions();
        var settings = new ReliabilitySettings(options);
        var commentWriter = new ReliabilityCommentWriter(settings);

        if (factsFile != null)
        {
            var facts = JsonNode.Parse(await File.ReadAllTextAsync(factsFile))!.AsObject();
            if (condensed)
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                await Console.Out.WriteLineAsync(commentWriter.Condense(facts).ToJsonString(jsonOptions));
                return 0;
            }
            var comment = await commentWriter.Write(facts);
            await Console.Out.WriteLineAsync(comment.Text);
            await Console.Error.WriteLineAsync($"--- {comment.Model}, {comment.TokensIn}/{comment.TokensOut} tokenu; pokusy: {string.Join(" | ", comment.Attempts)}");
            return 0;
        }
        var days = new List<DateOnly>();
        if (backfill > 0)
        {
            for (var d = backfill - 1; d >= 0; d--)
            {
                days.Add(settings.Yesterday.AddDays(-d));
            }
        }
        else
        {
            days.Add(day ?? settings.Yesterday);
        }

        var dbOptions = new DbContextOptionsBuilder<DiagDbContext>()
            .UseNpgsql(config.GetConnectionString("Diagnostics"))
            .Options;
        await using var db = new DiagDbContext(dbOptions);
        var repository = new ReliabilityRepository(db);
        if (!dryRun)
        {
            await db.Database.MigrateAsync();
        }
        else if ((await db.Database.GetPendingMigrationsAsync()).Any())
        {
            repository.UseHistory = false;
            await Console.Error.WriteLineAsync("Databaze nema vsechny migrace, historie se nepouzije.");
        }
        using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true));
        var runner = new ReliabilityReportRunner(settings, new GrafanaClient(settings), repository, commentWriter, loggerFactory.CreateLogger<ReliabilityReportRunner>());
        return await runner.Run(days, new ReliabilityRunOptions(dryRun, comments, output), CancellationToken.None) ? 0 : 1;
    }
}
