using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Import Letter of Credit — opened with a bank to pay a foreign <see cref="Supplier"/>
/// (beneficiary) for imported raw materials. Financial tracking only (no stock effect).
/// Lifecycle: Draft → Open → Shipped → Settled; cancellable while Draft or Open.
/// Currency-aware: <see cref="Amount"/> is in <see cref="Currency"/>; base BDT = Amount × ExchangeRate.
/// </summary>
public class LetterOfCredit : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;       // LC series (internal)
    public string LcNumber { get; set; } = string.Empty;   // the bank's LC number
    public string IssuingBank { get; set; } = string.Empty;

    /// <summary>
    /// Import (standalone) or Back-to-Back (opened against a master export LC to import raw
    /// materials for an export order). Both are import LCs with a supplier beneficiary.
    /// </summary>
    public LcType Type { get; set; } = LcType.Import;

    /// <summary>The master export LC number this B2B is backed by (the buyer's instrument).</summary>
    public string? MasterLcReference { get; set; }

    /// <summary>The buyer / applicant named on the master export LC.</summary>
    public string? MasterLcBuyer { get; set; }

    public int SupplierId { get; set; }                    // beneficiary
    public Supplier Supplier { get; set; } = null!;

    public long? PurchaseOrderId { get; set; }             // optional link to the PO it covers
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;        // BDT per 1 unit of Currency

    public decimal Amount { get; set; }                    // LC value, in Currency

    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public int TenorDays { get; set; }                     // payment tenor, e.g. 90 (days sight)

    public LcStatus Status { get; set; } = LcStatus.Draft;
    public DateOnly? ShipmentDate { get; set; }
    public DateOnly? SettlementDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>Phase A6a — financial events (margin, charges, retirement, interest, settlement).</summary>
    public ICollection<LcFinancialEvent> Events { get; set; } = new List<LcFinancialEvent>();
}

public enum LcStatus
{
    Draft = 1,
    Open = 2,
    Shipped = 3,
    Settled = 4,
    Cancelled = 5
}

public enum LcType
{
    Import = 1,        // standalone import LC
    BackToBack = 2     // import LC backed by a master export LC
}
