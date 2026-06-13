using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Configuration for the daily operational-alerts sweep (section "OperationalAlerts").
/// </summary>
public sealed class OperationalAlertsOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Notification channel for the alerts: InApp (record-only, shows in viewer) or Email.</summary>
    public string Channel { get; set; } = "InApp";

    /// <summary>Email address when Channel=Email; otherwise a label shown on the InApp record.</summary>
    public string Recipient { get; set; } = "Operations";

    /// <summary>Certificates expiring within this many days are flagged. Default 60.</summary>
    public int CertExpiryWithinDays { get; set; } = 60;

    /// <summary>
    /// Suppress a repeat of the SAME alert (same entity) if one was already raised within
    /// this many days. The job runs daily, so a persistent condition re-alerts every N days
    /// rather than every morning. Default 7.
    /// </summary>
    public int DedupDays { get; set; } = 7;
}

/// <summary>
/// Daily Hangfire job — scans for operational conditions that need a human nudge and raises
/// a notification for each (deduplicated against the recent past so it isn't spammed daily):
///   • Low stock — RawMaterial total on-hand ≤ MinimumStockLevel, or Product ≤ ReorderLevel.
///   • Overdue invoices — Issued/PartiallyPaid CustomerInvoices past their DueDate with a balance.
///   • Expiring compliance certificates — active certs within CertExpiryWithinDays of expiry (or expired).
///
/// Notifications go through <see cref="INotificationService"/> (same render/dispatch/audit path
/// as event-driven notifications); the job owns the single SaveChanges. Lives in Infrastructure
/// with direct DbContext access (same pattern as DatabaseBackupJob / AuditLogRetentionJob).
/// Also enqueueable on demand via POST /api/maintenance/run-operational-alerts.
/// </summary>
public class OperationalAlertsJob
{
    public const string RecurringJobId = "operational-alerts";

    // Dedup keys (stored in Notification.RelatedEntityType alongside the business entity id).
    private const string LowStockRm = "Alert:LowStockRM";
    private const string LowStockProduct = "Alert:LowStockProduct";
    private const string OverdueInvoice = "Alert:OverdueInvoice";
    private const string CertExpiry = "Alert:CertExpiry";

    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly OperationalAlertsOptions _opts;
    private readonly ILogger<OperationalAlertsJob> _logger;

    public OperationalAlertsJob(
        ApplicationDbContext db,
        INotificationService notifications,
        IOptions<OperationalAlertsOptions> opts,
        ILogger<OperationalAlertsJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!_opts.Enabled)
        {
            _logger.LogInformation("OperationalAlerts: disabled via configuration; skipping.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var dedupSince = DateTimeOffset.UtcNow.AddDays(-Math.Max(0, _opts.DedupDays));

        // Pull the set of (type,id) already alerted within the dedup window — one round-trip,
        // then in-memory membership checks per candidate.
        var recent = await _db.Notifications.AsNoTracking()
            .Where(n => n.CreatedAt >= dedupSince && n.RelatedEntityType != null
                     && (n.RelatedEntityType == LowStockRm
                      || n.RelatedEntityType == LowStockProduct
                      || n.RelatedEntityType == OverdueInvoice
                      || n.RelatedEntityType == CertExpiry))
            .Select(n => new { n.RelatedEntityType, n.RelatedEntityId })
            .ToListAsync(ct);
        var alreadyAlerted = recent
            .Select(r => (r.RelatedEntityType!, r.RelatedEntityId ?? 0))
            .ToHashSet();

        var raised = 0;
        raised += await RaiseLowStockRmAsync(alreadyAlerted, ct);
        raised += await RaiseLowStockProductAsync(alreadyAlerted, ct);
        raised += await RaiseOverdueInvoiceAsync(today, alreadyAlerted, ct);
        raised += await RaiseCertExpiryAsync(today, alreadyAlerted, ct);

        if (raised > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("OperationalAlerts: raised {Count} new alert(s).", raised);
        }
        else
        {
            _logger.LogInformation("OperationalAlerts: nothing to alert (all clear or already notified).");
        }
    }

    private async Task<int> RaiseLowStockRmAsync(HashSet<(string, long)> seen, CancellationToken ct)
    {
        // On-hand per RM = Σ StockOnHand.Quantity across warehouses. Compare to MinimumStockLevel.
        var onHand = await _db.StockOnHand.AsNoTracking()
            .Where(s => s.RawMaterialId != null)
            .GroupBy(s => s.RawMaterialId!.Value)
            .Select(g => new { RawMaterialId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.RawMaterialId, x => x.Qty, ct);

        var materials = await _db.RawMaterials.AsNoTracking()
            .Where(r => r.IsActive && r.MinimumStockLevel > 0)
            .Select(r => new { r.Id, r.Code, r.Name, r.MinimumStockLevel })
            .ToListAsync(ct);

        var count = 0;
        foreach (var rm in materials)
        {
            var qty = onHand.GetValueOrDefault(rm.Id, 0m);
            if (qty > rm.MinimumStockLevel) continue;
            if (!seen.Add((LowStockRm, rm.Id))) continue;

            await _notifications.NotifyAsync(
                _opts.Channel, _opts.Recipient,
                subject: $"Low stock: {rm.Code} {rm.Name}",
                body: $"Raw material {rm.Code} ({rm.Name}) on-hand {qty:0.####} is at/below the reorder level " +
                      $"{rm.MinimumStockLevel:0.####}. Consider raising a purchase requisition.",
                relatedType: LowStockRm, relatedId: rm.Id, ct: ct);
            count++;
        }
        return count;
    }

    private async Task<int> RaiseLowStockProductAsync(HashSet<(string, long)> seen, CancellationToken ct)
    {
        var onHand = await _db.StockOnHand.AsNoTracking()
            .Where(s => s.ProductId != null)
            .GroupBy(s => s.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.IsStockItem && p.ReorderLevel > 0)
            .Select(p => new { p.Id, p.Code, p.Name, p.ReorderLevel })
            .ToListAsync(ct);

        var count = 0;
        foreach (var p in products)
        {
            var qty = onHand.GetValueOrDefault(p.Id, 0m);
            if (qty > p.ReorderLevel) continue;
            if (!seen.Add((LowStockProduct, p.Id))) continue;

            await _notifications.NotifyAsync(
                _opts.Channel, _opts.Recipient,
                subject: $"Low stock: {p.Code} {p.Name}",
                body: $"Product {p.Code} ({p.Name}) on-hand {qty:0.####} is at/below the reorder level " +
                      $"{p.ReorderLevel:0.####}. Consider scheduling production.",
                relatedType: LowStockProduct, relatedId: p.Id, ct: ct);
            count++;
        }
        return count;
    }

    private async Task<int> RaiseOverdueInvoiceAsync(DateOnly today, HashSet<(string, long)> seen, CancellationToken ct)
    {
        var overdue = await _db.CustomerInvoices.AsNoTracking()
            .Where(i => (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                     && i.DueDate < today
                     && i.TotalAmount - i.AmountPaid > 0m)
            .Select(i => new
            {
                i.Id, i.Code, i.DueDate,
                CustomerName = i.Customer.Name,
                Due = i.TotalAmount - i.AmountPaid,
                Base = (i.TotalAmount - i.AmountPaid) * i.ExchangeRate
            })
            .ToListAsync(ct);

        var count = 0;
        foreach (var i in overdue)
        {
            if (!seen.Add((OverdueInvoice, i.Id))) continue;
            var daysLate = today.DayNumber - i.DueDate.DayNumber;

            await _notifications.NotifyAsync(
                _opts.Channel, _opts.Recipient,
                subject: $"Overdue invoice: {i.Code} ({i.CustomerName})",
                body: $"Invoice {i.Code} for {i.CustomerName} is {daysLate} day(s) overdue " +
                      $"(due {i.DueDate:yyyy-MM-dd}). Outstanding {i.Base:N2} BDT. Follow up for collection.",
                relatedType: OverdueInvoice, relatedId: i.Id, ct: ct);
            count++;
        }
        return count;
    }

    private async Task<int> RaiseCertExpiryAsync(DateOnly today, HashSet<(string, long)> seen, CancellationToken ct)
    {
        var cutoff = today.AddDays(Math.Max(0, _opts.CertExpiryWithinDays));
        var certs = await _db.ComplianceCertificates.AsNoTracking()
            .Where(c => c.IsActive && c.ExpiryDate <= cutoff)
            .Select(c => new { c.Id, c.Name, c.CertificateNumber, c.ExpiryDate })
            .ToListAsync(ct);

        var count = 0;
        foreach (var c in certs)
        {
            if (!seen.Add((CertExpiry, c.Id))) continue;
            var daysLeft = c.ExpiryDate.DayNumber - today.DayNumber;
            var phrase = daysLeft < 0 ? $"EXPIRED {-daysLeft} day(s) ago" : $"expires in {daysLeft} day(s)";
            var numberSuffix = string.IsNullOrWhiteSpace(c.CertificateNumber) ? "" : $" (#{c.CertificateNumber})";

            await _notifications.NotifyAsync(
                _opts.Channel, _opts.Recipient,
                subject: $"Certificate {phrase}: {c.Name}",
                body: $"Compliance certificate '{c.Name}'{numberSuffix} {phrase} " +
                      $"(expiry {c.ExpiryDate:yyyy-MM-dd}). Arrange renewal to stay audit-ready.",
                relatedType: CertExpiry, relatedId: c.Id, ct: ct);
            count++;
        }
        return count;
    }
}
