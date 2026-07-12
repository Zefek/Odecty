using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OdectyStat1.Dto;

namespace OdectyStat1.DataLayer;

public class GarageTimeoutService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly GarageHandshakeStore store;
    private readonly IOptions<GarageSettings> options;
    private readonly ILogger<GarageTimeoutService> logger;

    public GarageTimeoutService(
        IServiceScopeFactory scopeFactory,
        GarageHandshakeStore store,
        IOptions<GarageSettings> options,
        ILogger<GarageTimeoutService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.store = store;
        this.options = options;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ttl = TimeSpan.FromSeconds(options.Value.SlotTtlSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var pending in store.Snapshot())
            {
                if (DateTime.UtcNow - pending.RequestedAt <= ttl)
                {
                    continue;
                }
                store.Remove(pending.CorrelationId);
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
                    var record = await db.GarageCommands
                        .Where(c => c.CorrelationId == pending.CorrelationId && c.Status == "pending")
                        .OrderByDescending(c => c.Id)
                        .FirstOrDefaultAsync(stoppingToken);
                    if (record != null)
                    {
                        record.Status = "timeout";
                        record.CompletedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    logger.LogWarning("Garage handshake timed out R={R} identity={Identity}", pending.CorrelationId, pending.Identity);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to record garage handshake timeout R={R}", pending.CorrelationId);
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
