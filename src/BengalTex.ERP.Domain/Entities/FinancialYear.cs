using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A fiscal year (user-defined range — supports both Jul–Jun and Jan–Dec conventions).
/// Creating a year auto-generates its 12 monthly <see cref="AccountingPeriod"/>s. Closing a
/// year posts the year-end closing voucher (P&amp;L → Retained Earnings) and freezes it;
/// reopening (audited) reverses that voucher. Phase A1 — Fiscal Rails.
/// </summary>
public class FinancialYear : BaseEntity
{
    /// <summary>Display code, e.g. "FY2026-27" or "FY2026".</summary>
    public string Code { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public FinancialYearStatus Status { get; set; } = FinancialYearStatus.Open;

    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<AccountingPeriod> Periods { get; set; } = new List<AccountingPeriod>();
}

/// <summary>
/// One monthly posting period of a <see cref="FinancialYear"/>. The period status drives the
/// posting guard: Open — everything posts; SoftClosed — auto-journals still post, manual
/// vouchers need the CloseBooks permission; Locked — nothing posts (reopen is audited).
/// </summary>
public class AccountingPeriod : BaseEntity
{
    public int FinancialYearId { get; set; }
    public FinancialYear FinancialYear { get; set; } = null!;

    /// <summary>1-based month position within the year (1–12).</summary>
    public int PeriodNumber { get; set; }

    /// <summary>Display name, e.g. "Jul 2026".</summary>
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;

    public DateTimeOffset? StatusChangedAt { get; set; }
    public string? StatusChangedBy { get; set; }
}

public enum FinancialYearStatus
{
    Open = 1,
    Closed = 2
}

public enum AccountingPeriodStatus
{
    Open = 1,
    SoftClosed = 2,   // auto-journals allowed; manual vouchers require Accounting.CloseBooks
    Locked = 3        // no postings at all; reopen is audited
}
