using BengalTex.ERP.Application.Reports.Commands;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Application.Reports.Jobs;

/// <summary>
/// Configuration for the month-end statement batch (section "MonthlyStatements").
/// DISABLED by default — auto-emailing every customer is a conscious business decision;
/// flip Enabled to true once the factory wants it.
/// </summary>
public sealed class MonthlyStatementOptions
{
    public bool Enabled { get; set; }

    /// <summary>Email AR statements to customers with activity in the month.</summary>
    public bool IncludeCustomers { get; set; } = true;

    /// <summary>Email AP statements to suppliers with activity in the month. Off by default — supplier statements are usually sent on demand.</summary>
    public bool IncludeSuppliers { get; set; }

    /// <summary>Internal Cc on every statement (e.g. accounts inbox) — optional.</summary>
    public string? CcAddresses { get; set; }
}

/// <summary>
/// Hangfire job — on the 1st of each month, emails the previous calendar month's
/// Statement of Account (PDF attached) to every ACTIVE party that (a) had activity
/// in that month (invoice or receipt/payment) and (b) has an email address on file.
/// Reuses <see cref="SendCustomerStatementEmailCommand"/> /
/// <see cref="SendSupplierStatementEmailCommand"/> per party, so every send hits the
/// same render + audit path as a manual send from the statement screen.
/// Parties WITHOUT activity are skipped even if they carry a balance — this is a
/// monthly movement statement, not a dunning reminder (that's a separate feature).
/// </summary>
public class MonthlyStatementBatchJob
{
    public const string RecurringJobId = "monthly-statements";

    private readonly IMediator _mediator;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _custInvRepo;
    private readonly IRepository<Domain.Entities.Receipt, long> _receiptRepo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _suppInvRepo;
    private readonly IRepository<Domain.Entities.Payment, long> _paymentRepo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly MonthlyStatementOptions _opts;
    private readonly ILogger<MonthlyStatementBatchJob> _logger;

    public MonthlyStatementBatchJob(
        IMediator mediator,
        IRepository<Domain.Entities.CustomerInvoice, long> custInvRepo,
        IRepository<Domain.Entities.Receipt, long> receiptRepo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> suppInvRepo,
        IRepository<Domain.Entities.Payment, long> paymentRepo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IOptions<MonthlyStatementOptions> opts,
        ILogger<MonthlyStatementBatchJob> logger)
    {
        _mediator = mediator;
        _custInvRepo = custInvRepo; _receiptRepo = receiptRepo; _customerRepo = customerRepo;
        _suppInvRepo = suppInvRepo; _paymentRepo = paymentRepo; _supplierRepo = supplierRepo;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!_opts.Enabled)
        {
            _logger.LogInformation("MonthlyStatements: disabled via configuration; skipping.");
            return;
        }

        // Window = previous calendar month relative to the run date.
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var toDate = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        var fromDate = new DateOnly(toDate.Year, toDate.Month, 1);
        _logger.LogInformation("MonthlyStatements: starting batch for {From} → {To}.", fromDate, toDate);

        if (_opts.IncludeCustomers) await SendCustomerStatementsAsync(fromDate, toDate, ct);
        if (_opts.IncludeSuppliers) await SendSupplierStatementsAsync(fromDate, toDate, ct);
    }

    private async Task SendCustomerStatementsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct)
    {
        var fromInvoices = await _custInvRepo.Query()
            .Where(i => i.Status != CustomerInvoiceStatus.Draft
                     && i.Status != CustomerInvoiceStatus.Cancelled
                     && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
            .Select(i => i.CustomerId).Distinct().ToListAsync(ct);
        var fromReceipts = await _receiptRepo.Query()
            .Where(r => r.ReceiptDate >= fromDate && r.ReceiptDate <= toDate)
            .Select(r => r.CustomerInvoice.CustomerId).Distinct().ToListAsync(ct);
        var activeIds = fromInvoices.Union(fromReceipts).ToList();
        if (activeIds.Count == 0)
        {
            _logger.LogInformation("MonthlyStatements: no customer activity in window; nothing to send.");
            return;
        }

        var recipients = await _customerRepo.Query()
            .Where(c => activeIds.Contains(c.Id) && c.IsActive)
            .Select(c => new { c.Id, c.Name, c.Email })
            .ToListAsync(ct);

        int sent = 0, skipped = 0, failed = 0;
        foreach (var c in recipients)
        {
            if (string.IsNullOrWhiteSpace(c.Email) || !c.Email.Contains('@')) { skipped++; continue; }
            try
            {
                var res = await _mediator.Send(new SendCustomerStatementEmailCommand(
                    c.Id, fromDate, toDate,
                    c.Email, _opts.CcAddresses,
                    $"Statement of Account ({fromDate:MMMM yyyy})",
                    $"<p>Dear {c.Name},</p>" +
                    $"<p>Please find attached your statement of account for <strong>{fromDate:MMMM yyyy}</strong>.</p>" +
                    "<p>Kindly reconcile and report any discrepancy within 7 days.</p>" +
                    "<p>Regards,<br/>Accounts Team</p>"), ct);
                if (res.Success) sent++; else failed++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "MonthlyStatements: customer {CustomerId} failed.", c.Id);
            }
        }
        _logger.LogInformation(
            "MonthlyStatements: customers done — {Sent} sent, {Skipped} skipped (no email), {Failed} failed of {Total} active.",
            sent, skipped, failed, recipients.Count);
    }

    private async Task SendSupplierStatementsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct)
    {
        var fromInvoices = await _suppInvRepo.Query()
            .Where(i => i.Status != SupplierInvoiceStatus.Draft
                     && i.Status != SupplierInvoiceStatus.Cancelled
                     && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
            .Select(i => i.SupplierId).Distinct().ToListAsync(ct);
        var fromPayments = await _paymentRepo.Query()
            .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .Select(p => p.SupplierInvoice.SupplierId).Distinct().ToListAsync(ct);
        var activeIds = fromInvoices.Union(fromPayments).ToList();
        if (activeIds.Count == 0)
        {
            _logger.LogInformation("MonthlyStatements: no supplier activity in window; nothing to send.");
            return;
        }

        var recipients = await _supplierRepo.Query()
            .Where(s => activeIds.Contains(s.Id) && s.IsActive)
            .Select(s => new { s.Id, s.Name, s.Email })
            .ToListAsync(ct);

        int sent = 0, skipped = 0, failed = 0;
        foreach (var s in recipients)
        {
            if (string.IsNullOrWhiteSpace(s.Email) || !s.Email.Contains('@')) { skipped++; continue; }
            try
            {
                var res = await _mediator.Send(new SendSupplierStatementEmailCommand(
                    s.Id, fromDate, toDate,
                    s.Email, _opts.CcAddresses,
                    $"Payable Statement ({fromDate:MMMM yyyy})",
                    $"<p>Dear {s.Name},</p>" +
                    $"<p>Please find attached our statement of account with you for <strong>{fromDate:MMMM yyyy}</strong>.</p>" +
                    "<p>Kindly reconcile against your ledger and report any discrepancy within 7 days.</p>" +
                    "<p>Regards,<br/>Accounts Team</p>"), ct);
                if (res.Success) sent++; else failed++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "MonthlyStatements: supplier {SupplierId} failed.", s.Id);
            }
        }
        _logger.LogInformation(
            "MonthlyStatements: suppliers done — {Sent} sent, {Skipped} skipped (no email), {Failed} failed of {Total} active.",
            sent, skipped, failed, recipients.Count);
    }
}
