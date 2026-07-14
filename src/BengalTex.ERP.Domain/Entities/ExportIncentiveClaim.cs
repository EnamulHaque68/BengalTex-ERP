using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Phase A6b — a government export cash-incentive (subsidy) claim on realized exports. Accrued when
/// filed (Dr 1186 Export Incentive Receivable / Cr 4260 Export Incentive Income) and cleared when the
/// bank credits the incentive (Dr Bank / Cr 1186). Optionally linked to the export customer invoice.
/// </summary>
public class ExportIncentiveClaim : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;        // "EI-####"

    /// <summary>Optional link to the export customer invoice the incentive is claimed against.</summary>
    public long? CustomerInvoiceId { get; set; }
    public CustomerInvoice? CustomerInvoice { get; set; }

    /// <summary>Form-EXP / export bill / proceeds-realization reference (free text).</summary>
    public string? ExportReference { get; set; }

    /// <summary>Incentive rate applied, in percent (informational — Amount is authoritative).</summary>
    public decimal IncentiveRate { get; set; }

    /// <summary>The incentive amount claimed, in BDT.</summary>
    public decimal Amount { get; set; }

    public DateOnly ClaimDate { get; set; }

    public IncentiveClaimStatus Status { get; set; } = IncentiveClaimStatus.Accrued;

    public DateOnly? ReceivedDate { get; set; }
    public PaymentMethod? ReceivedMethod { get; set; }
    public string? BankReference { get; set; }

    public string? Notes { get; set; }
}

public enum IncentiveClaimStatus
{
    Accrued = 1,     // filed — receivable booked
    Received = 2,    // bank credited the incentive
    Cancelled = 3    // accrual reversed
}
