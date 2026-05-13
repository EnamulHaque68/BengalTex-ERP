using System.Text.Json;
using BengalTex.ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Hangfire recurring job that drains the OutboxMessages table.
/// Each message is a JSON-serialized MediatR INotification stored in the same
/// SQL transaction as a business state change (transactional outbox pattern).
///
/// Convention (producer side):
///   var msg = new OutboxMessage {
///       Type = typeof(MyEvent).AssemblyQualifiedName!,
///       Payload = JsonSerializer.Serialize(myEvent),
///       OccurredOn = DateTimeOffset.UtcNow
///   };
///   _db.OutboxMessages.Add(msg);
///   _db.SaveChangesAsync(ct); // commits with the business change
///
/// Scheduling: registered as a Hangfire recurring job in Program.cs.
/// Single-server deployment assumption — if multiple servers run this concurrently,
/// add row-level locking (UPDLOCK + READPAST) on the SELECT to avoid duplicate dispatch.
/// </summary>
public class OutboxProcessor
{
    private const int BatchSize = 100;
    private const int MaxRetries = 5;

    public const string RecurringJobId = "outbox-processor";

    private readonly ApplicationDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        ApplicationDbContext db,
        IPublisher publisher,
        ILogger<OutboxProcessor> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessAsync(CancellationToken ct = default)
    {
        var pending = await _db.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogInformation("OutboxProcessor: dispatching {Count} message(s)", pending.Count);

        foreach (var msg in pending)
        {
            try
            {
                var type = Type.GetType(msg.Type);
                if (type is null)
                {
                    msg.Error = $"Type not found: {msg.Type}";
                    msg.RetryCount++;
                    _logger.LogWarning("Outbox message {Id}: type '{Type}' not resolved", msg.Id, msg.Type);
                    continue;
                }

                var notification = JsonSerializer.Deserialize(msg.Payload, type);
                if (notification is null)
                {
                    msg.Error = "Payload deserialized to null.";
                    msg.RetryCount++;
                    continue;
                }

                await _publisher.Publish(notification, ct);

                msg.ProcessedOn = DateTimeOffset.UtcNow;
                msg.Error = null;
            }
            catch (Exception ex)
            {
                msg.Error = ex.Message;
                msg.RetryCount++;
                _logger.LogError(ex,
                    "Failed to process outbox message {Id} (attempt {Attempt}/{Max})",
                    msg.Id, msg.RetryCount, MaxRetries);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
