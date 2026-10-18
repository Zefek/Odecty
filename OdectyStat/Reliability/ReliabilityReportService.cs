using System.Globalization;

namespace OdectyStat1.Reliability;

public class ReliabilityReportService : BackgroundService
{
    readonly IServiceScopeFactory scopeFactory;
    readonly ReliabilitySettings settings;
    readonly ILogger<ReliabilityReportService> logger;

    public ReliabilityReportService(IServiceScopeFactory scopeFactory, ReliabilitySettings settings, ILogger<ReliabilityReportService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.settings = settings;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var runAt = TimeOnly.Parse(settings.Options.RunAt, CultureInfo.InvariantCulture);
        if (settings.Options.RunOnStartup)
        {
            await RunYesterday(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var next = now.Date + runAt.ToTimeSpan();
            if (next <= now)
            {
                next = next.AddDays(1);
            }
            logger.LogInformation("Next reliability report at {Next}", next);
            try
            {
                await Task.Delay(next - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            await RunYesterday(stoppingToken);
        }
    }

    async Task RunYesterday(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<ReliabilityReportRunner>();
        await runner.Run([settings.Yesterday], new ReliabilityRunOptions(false, true, null), stoppingToken);
    }
}
