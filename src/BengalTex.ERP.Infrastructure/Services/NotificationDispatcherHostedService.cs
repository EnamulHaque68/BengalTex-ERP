using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Background scanner that fires date-based notifications once per day. Runs an initial
/// scan ~10 seconds after startup and then every 24 hours. Each scan:
///   * Compliance certificates expiring in the next 60 days → notify HRManager
///   * Open / InProgress audit findings whose DueDate has passed → notify AssignedToEmployee
///   * RawMaterial / Product with total stock-on-hand below threshold → notify ProductionManager
///     (RawMaterial uses MinimumStockLevel; Product uses ReorderLevel; only items with
///     threshold > 0 trigger — 0 means no alert configured)
/// Idempotency: a new notification is only created if no prior Notification with the same
/// (RelatedEntityType, RelatedEntityId) was sent within the last 7 days — prevents spam.
/// </summary>
public sealed class NotificationDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDispatcherHostedService> _logger;

    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int CertExpiringSoonDays = 60;
    private const int DedupeWindowDays = 7;

    public NotificationDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunScanAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "NotificationDispatcher scan failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunScanAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var clock = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IDateTimeProvider>();

        var today = clock.Today;
        var soonCutoff = today.AddDays(CertExpiringSoonDays);
        var dedupeCutoff = clock.UtcNow.AddDays(-DedupeWindowDays);

        var created = 0;

        // ── Compliance: certificates expiring soon or already expired ──
        var expiringCerts = await db.ComplianceCertificates
            .Where(c => c.IsActive && c.ExpiryDate <= soonCutoff)
            .Select(c => new { c.Id, c.Name, c.CertificateType, c.ExpiryDate })
            .ToListAsync(ct);
        foreach (var c in expiringCerts)
        {
            var alreadySent = await db.Notifications.AnyAsync(n =>
                n.RelatedEntityType == "ComplianceCertificate" && n.RelatedEntityId == c.Id
                && n.CreatedAt >= dedupeCutoff, ct);
            if (alreadySent) continue;

            var days = c.ExpiryDate.DayNumber - today.DayNumber;
            var subj = days < 0
                ? $"Certificate EXPIRED: {c.Name}"
                : $"Certificate expiring in {days}d: {c.Name}";
            var body = days < 0
                ? $"{c.CertificateType} '{c.Name}' expired on {c.ExpiryDate:yyyy-MM-dd} ({-days} day(s) ago). Renew immediately."
                : $"{c.CertificateType} '{c.Name}' expires on {c.ExpiryDate:yyyy-MM-dd} ({days} day(s) remaining).";
            await notifications.NotifyAsync(
                NotificationChannels.InApp, recipient: "HRManager",
                subject: subj, body: body,
                relatedType: "ComplianceCertificate", relatedId: c.Id, ct: ct);
            created++;
        }

        // ── Compliance: overdue CAP findings ──
        var overdue = await db.AuditFindings
            .Where(f => (f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress)
                     && f.DueDate.HasValue && f.DueDate.Value < today)
            .Select(f => new
            {
                f.Id, f.FindingDescription, f.Severity, f.DueDate, f.AssignedToEmployeeId,
                AssignedName = f.AssignedToEmployee != null ? f.AssignedToEmployee.FullName : null,
                AuditCode = f.ComplianceAudit.Code
            })
            .ToListAsync(ct);
        foreach (var f in overdue)
        {
            var alreadySent = await db.Notifications.AnyAsync(n =>
                n.RelatedEntityType == "AuditFinding" && n.RelatedEntityId == f.Id
                && n.CreatedAt >= dedupeCutoff, ct);
            if (alreadySent) continue;

            var recipient = f.AssignedName ?? "HRManager";
            var daysLate = today.DayNumber - f.DueDate!.Value.DayNumber;
            await notifications.NotifyAsync(
                NotificationChannels.InApp, recipient: recipient,
                subject: $"OVERDUE CAP item ({f.AuditCode})",
                body: $"{f.Severity} finding on audit {f.AuditCode} was due {f.DueDate:yyyy-MM-dd} " +
                      $"({daysLate} day(s) overdue): {f.FindingDescription}",
                relatedType: "AuditFinding", relatedId: f.Id, ct: ct);
            created++;
        }

        // ── Inventory: raw materials below MinimumStockLevel ──
        // Sum StockOnHand per RM across all warehouses; compare against the threshold.
        var rmBelow = await db.RawMaterials
            .Where(r => r.IsActive && r.MinimumStockLevel > 0m)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                r.MinimumStockLevel,
                Uom = r.UnitOfMeasure.Code,
                OnHand = db.StockOnHand
                    .Where(s => s.RawMaterialId == r.Id)
                    .Sum(s => (decimal?)s.Quantity) ?? 0m
            })
            .Where(x => x.OnHand < x.MinimumStockLevel)
            .ToListAsync(ct);

        foreach (var r in rmBelow)
        {
            var alreadySent = await db.Notifications.AnyAsync(n =>
                n.RelatedEntityType == "RawMaterial" && n.RelatedEntityId == r.Id
                && n.CreatedAt >= dedupeCutoff, ct);
            if (alreadySent) continue;

            await notifications.NotifyAsync(
                NotificationChannels.InApp, recipient: "ProductionManager",
                subject: $"Low stock: {r.Name} ({r.Code})",
                body: $"Raw material '{r.Name}' is below re-order level. " +
                      $"On-hand: {r.OnHand:0.####} {r.Uom}, threshold: {r.MinimumStockLevel:0.####} {r.Uom}. " +
                      $"Consider raising a Purchase Order.",
                relatedType: "RawMaterial", relatedId: r.Id, ct: ct);
            created++;
        }

        // ── Inventory: finished products below ReorderLevel ──
        var prodBelow = await db.Products
            .Where(p => p.IsActive && p.IsStockItem && p.ReorderLevel > 0m)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.ReorderLevel,
                Uom = p.UnitOfMeasure.Code,
                OnHand = db.StockOnHand
                    .Where(s => s.ProductId == p.Id)
                    .Sum(s => (decimal?)s.Quantity) ?? 0m
            })
            .Where(x => x.OnHand < x.ReorderLevel)
            .ToListAsync(ct);

        foreach (var p in prodBelow)
        {
            var alreadySent = await db.Notifications.AnyAsync(n =>
                n.RelatedEntityType == "Product" && n.RelatedEntityId == p.Id
                && n.CreatedAt >= dedupeCutoff, ct);
            if (alreadySent) continue;

            await notifications.NotifyAsync(
                NotificationChannels.InApp, recipient: "ProductionManager",
                subject: $"Low stock: {p.Name} ({p.Code})",
                body: $"Finished product '{p.Name}' is below re-order level. " +
                      $"On-hand: {p.OnHand:0.####} {p.Uom}, threshold: {p.ReorderLevel:0.####} {p.Uom}. " +
                      $"Consider raising a Production Order.",
                relatedType: "Product", relatedId: p.Id, ct: ct);
            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("NotificationDispatcher created {Count} notification(s)", created);
        }
    }
}
