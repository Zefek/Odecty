using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OdectyStat1.Business;
using OdectyStat1.Contracts;
using OdectyStat1.DataLayer;
using OdectyStat1.Dto;

namespace OdectyStat1.Application;

public class GarageCommandService : IGarageCommandService
{
    private readonly IMessageQueue messageQueue;
    private readonly IGarageCommandSigner signer;
    private readonly GarageHandshakeStore store;
    private readonly DiagDbContext db;
    private readonly IOptions<GarageSettings> options;
    private readonly ILogger<GarageCommandService> logger;

    private static string SanitizeForLog(string value)
    {
        return value.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    public GarageCommandService(
        IMessageQueue messageQueue,
        IGarageCommandSigner signer,
        GarageHandshakeStore store,
        DiagDbContext db,
        IOptions<GarageSettings> options,
        ILogger<GarageCommandService> logger)
    {
        this.messageQueue = messageQueue;
        this.signer = signer;
        this.store = store;
        this.db = db;
        this.options = options;
        this.logger = logger;
    }

    public async Task<uint> RequestOpen(string identity, CancellationToken cancellationToken)
    {
        var r = (uint)RandomNumberGenerator.GetInt32(1, int.MaxValue);
        db.GarageCommands.Add(new GarageCommand
        {
            CorrelationId = r,
            Identity = identity,
            RequestedAt = DateTime.UtcNow,
            Status = "pending"
        });
        await db.SaveChangesAsync(cancellationToken);

        store.Add(new PendingGarageRequest
        {
            CorrelationId = r,
            Identity = identity,
            RequestedAt = DateTime.UtcNow
        });

        var request = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(request, r);
        await messageQueue.MQTTPublish(request, options.Value.RequestTopic);
        logger.LogInformation("Garage handshake started R={R} identity={Identity}", r, SanitizeForLog(identity));
        return r;
    }

    public async Task HandleChallenge(uint correlationId, ReadOnlyMemory<byte> nonce, CancellationToken cancellationToken)
    {
        if (!store.TryGet(correlationId, out var pending))
        {
            logger.LogWarning("Challenge for unknown or expired R={R} ignored", correlationId);
            return;
        }

        var signature = signer.Sign(correlationId, nonce.Span);
        var response = new byte[4 + nonce.Length + signature.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(response, correlationId);
        nonce.Span.CopyTo(response.AsSpan(4));
        signature.CopyTo(response.AsSpan(4 + nonce.Length));
        await messageQueue.MQTTPublish(response, options.Value.ResponseTopic);
        logger.LogInformation("Garage response signed R={R} identity={Identity}", correlationId, SanitizeForLog(pending.Identity));
    }

    public async Task HandleResult(uint correlationId, byte status, CancellationToken cancellationToken)
    {
        store.Remove(correlationId);

        var text = status switch
        {
            1 => "opened",
            2 => "expired",
            3 => "badsig",
            _ => $"unknown({status})"
        };

        var record = await db.GarageCommands
            .Where(c => c.CorrelationId == correlationId && c.Status == "pending")
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (record != null)
        {
            record.Status = text;
            record.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        logger.LogInformation("Garage result R={R} status={Status}", correlationId, text);
    }
}
