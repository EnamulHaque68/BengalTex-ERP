using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Application.Reports.Jobs;

/// <summary>
/// Configuration for the dunning (overdue-payment reminder) job, section "Dunning".
/// DISABLED by default — emailing customers about overdue payments is a business decision;
/// flip Enabled to true once the factory wants automated collections reminders.
/// </summary>
public sealed class DunningOptions
{
    public bool Enabled { get; set; }

    /// <summary>Days overdue at/after which a gentle first reminder goes out. Below this, nothing.</summary>
    public int FirstReminderDays { get; set; } = 7;

    /// <summary>Days overdue at/after which the tone escalates to a "second notice".</summary>
    public int SecondNoticeDays { get; set; } = 30;

    /// <summary>Days overdue at/after which the tone escalates to a "final notice".</summary>
    public int FinalNoticeDays { get; set; } = 60;

    /// <summary>Internal Cc on every reminder (e.g. accounts/collections inbox) — optional.</summary>
    public string? CcAddresses { get; set; }

    /// <summary>
    /// Suppress a repeat reminder for the SAME invoice within this many days. The job runs
    /// daily, so a still-unpaid invoice is re-reminded every N days with escalating tone as
    /// it ages past the tier thresholds. Default 7.
    /// </summary>
    public int DedupDays { get; set; } = 7;
}

/// <summary>
/// Daily Hangfire job — emails customers a payment reminder for each of their overdue
/// invoices (Issued/PartiallyPaid, past DueDate, balance &gt; 0), with the tone escalating
/// by how overdue the invoice is (reminder → second notice → final notice). Complements the
/// internal-facing overdue-invoice operational alert (OperationalAlertsJob), which nudges
/// staff; this one reaches the CUSTOMER. Every send is logged to <see cref="SentEmail"/>
/// (SourceType="Dunning"); a reminder is suppressed if one was sent for the same invoice
/// within DedupDays. OPT-IN — does nothing until Dunning:Enabled=true and a real Email
/// provider is configured (DevLogger only logs).
/// </summary>
public class DunningReminderJob
{
    public const string RecurringJobId = "dunning-reminders";

    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IRepository<SentEmail, long> _sentRepo;
    private readonly IRepository<Domain.Entities.Company> _companyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;
    private readonly DunningOptions _opts;
    private readonly ILogger<DunningReminderJob> _logger;

    public DunningReminderJob(
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IRepository<SentEmail, long> sentRepo,
        IRepository<Domain.Entities.Company> companyRepo,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IOptions<DunningOptions> opts,
        ILogger<DunningReminderJob> logger)
    {
        _invRepo = invRepo; _sentRepo = sentRepo; _companyRepo = companyRepo;
        _uow = uow; _emailSender = emailSender; _opts = opts.Value; _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!_opts.Enabled)
        {
            _logger.LogInformation("Dunning: disabled via configuration; skipping.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var firstCutoff = today.AddDays(-Math.Max(0, _opts.FirstReminderDays));
        var dedupSince = DateTimeOffset.UtcNow.AddDays(-Math.Max(0, _opts.DedupDays));

        // Candidate invoices: overdue by at least FirstReminderDays, still owing.
        var overdue = await _invRepo.Query()
            .Where(i => (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                     && i.DueDate <= firstCutoff
                     && i.TotalAmount - i.AmountPaid > 0m)
            .Select(i => new
            {
                i.Id, i.Code, i.DueDate,
                CustomerName = i.Customer.Name,
                CustomerEmail = i.Customer.Email,
                CurrencyCode = i.Currency.Code,
                Due = i.TotalAmount - i.AmountPaid
            })
            .ToListAsync(ct);

        if (overdue.Count == 0)
        {
            _logger.LogInformation("Dunning: no overdue invoices past the first-reminder threshold.");
            return;
        }

        // Invoices reminded within the dedup window — one query, then in-memory skip.
        var recentlyReminded = (await _sentRepo.Query()
            .Where(e => e.SourceType == "Dunning" && e.SentAt >= dedupSince && e.SourceId != null)
            .Select(e => e.SourceId!.Value)
            .ToListAsync(ct)).ToHashSet();

        var company = await _companyRepo.Query().AsNoTracking().FirstOrDefaultAsync(ct);
        var companyName = company?.Name ?? "Our Company";

        int sent = 0, skipped = 0, failed = 0;
        foreach (var inv in overdue)
        {
            if (recentlyReminded.Contains(inv.Id)) { skipped++; continue; }
            if (string.IsNullOrWhiteSpace(inv.CustomerEmail) || !inv.CustomerEmail.Contains('@')) { skipped++; continue; }

            var daysLate = today.DayNumber - inv.DueDate.DayNumber;
            var (tier, subjectVerb, toneLine) = ClassifyTier(daysLate);
            var amount = $"{inv.Due:N2} {inv.CurrencyCode}";

            var subject = $"{subjectVerb}: invoice {inv.Code} ({daysLate} days overdue)";
            var body =
                $"<p>Dear {inv.CustomerName},</p>" +
                $"<p>Our records show invoice <strong>{inv.Code}</strong> (due {inv.DueDate:yyyy-MM-dd}) " +
                $"remains unpaid — now <strong>{daysLate} day(s) overdue</strong> with an outstanding balance of " +
                $"<strong>{amount}</strong>.</p>" +
                $"<p>{toneLine}</p>" +
                $"<p>If payment has already been made, please disregard this notice and share the remittance details " +
                $"so we can reconcile our records.</p>" +
                $"<p>Regards,<br/>Accounts Receivable<br/>{companyName}</p>";

            var entity = new SentEmail
            {
                SentAt = DateTimeOffset.UtcNow,
                SentByUser = "system",
                SourceType = "Dunning",
                SourceId = inv.Id,
                SourceCode = inv.Code,
                ToAddresses = inv.CustomerEmail!,
                CcAddresses = string.IsNullOrWhiteSpace(_opts.CcAddresses) ? null : _opts.CcAddresses,
                Subject = subject,
                Body = body,
                Status = SentEmailStatus.Sent
            };

            try
            {
                var recipients = new List<string> { inv.CustomerEmail! };
                if (!string.IsNullOrWhiteSpace(_opts.CcAddresses))
                    recipients.AddRange(_opts.CcAddresses.Split(new[] { ',', ';' },
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                await _emailSender.SendAsync(recipients, subject, body, ct);
            }
            catch (Exception ex)
            {
                entity.Status = SentEmailStatus.Failed;
                entity.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                failed++;
                _logger.LogError(ex, "Dunning: send failed for invoice {Code}", inv.Code);
            }

            await _sentRepo.AddAsync(entity, ct);
            recentlyReminded.Add(inv.Id);   // guard against same-run dupes
            if (entity.Status == SentEmailStatus.Sent) sent++;
            _logger.LogInformation("Dunning: {Tier} reminder queued for {Code} ({Days}d overdue).", tier, inv.Code, daysLate);
        }

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Dunning: done — {Sent} sent, {Skipped} skipped, {Failed} failed of {Total} overdue.",
            sent, skipped, failed, overdue.Count);
    }

    private (string Tier, string SubjectVerb, string ToneLine) ClassifyTier(int daysLate)
    {
        if (daysLate >= _opts.FinalNoticeDays)
            return ("Final",
                "FINAL NOTICE",
                "This is a final reminder. Please arrange immediate payment to avoid a hold on further orders and possible escalation.");
        if (daysLate >= _opts.SecondNoticeDays)
            return ("Second",
                "Second notice — payment overdue",
                "We have not yet received payment. Please settle this invoice at the earliest, or contact us if there is an issue.");
        return ("First",
            "Payment reminder",
            "This is a friendly reminder to settle the above invoice at your earliest convenience.");
    }
}
