using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Configurable retention for the AuditLogEntries table.
/// Default 365 days; tune per deployment via Application:AuditLogRetentionDays.
/// Set to 0 (or negative) to disable the trim job entirely.
/// </summary>
public sealed class AuditLogRetentionOptions
{
    public int RetentionDays { get; set; } = 365;

    /// <summary>Per-pass batch size. Keeps tx log + locks bounded on large purges.</summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>Safety cap so an unbounded backlog can't run forever in one nightly window.</summary>
    public int MaxBatchesPerRun { get; set; } = 200;
}

/// <summary>
/// Nightly Hangfire job — deletes AuditLogEntries older than the configured retention.
/// Batches the delete (BatchSize rows at a time) so a multi-year backlog doesn't blow up
/// the SQL Server transaction log or hold long write locks. Uses the IX_AuditLogEntries_Timestamp
/// index (already present since InitialCreate) so the cutoff scan is cheap.
///
/// Scheduling: registered as a Hangfire recurring job in Program.cs at 02:30 daily.
/// Single-server deployment assumption (same as OutboxProcessor).
/// </summary>
public class AuditLogRetentionJob
{
    public const string RecurringJobId = "audit-log-retention";

    private readonly ApplicationDbContext _db;
    private readonly AuditLogRetentionOptions _opts;
    private readonly ILogger<AuditLogRetentionJob> _logger;

    public AuditLogRetentionJob(
        ApplicationDbContext db,
        IOptions<AuditLogRetentionOptions> opts,
        ILogger<AuditLogRetentionJob> logger)
    {
        _db = db;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (_opts.RetentionDays <= 0)
        {
            _logger.LogInformation("AuditLogRetention: disabled (RetentionDays={Days}); skipping.", _opts.RetentionDays);
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-_opts.RetentionDays);
        var totalDeleted = 0;

        for (var batch = 0; batch < _opts.MaxBatchesPerRun; batch++)
        {
            ct.ThrowIfCancellationRequested();

            // ExecuteDeleteAsync issues a single DELETE TOP(BatchSize) … WHERE Timestamp < cutoff
            // — no entity tracking, no SaveChanges round-trip.
            var deleted = await _db.AuditLogEntries
                .Where(a => a.Timestamp < cutoff)
                .OrderBy(a => a.Timestamp)
                .Take(_opts.BatchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;
            if (deleted < _opts.BatchSize) break;     // last batch — backlog drained
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "AuditLogRetention: deleted {Count} rows older than {Cutoff:O} (retention {Days} days).",
                totalDeleted, cutoff, _opts.RetentionDays);
        }
    }
}
