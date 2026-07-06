using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Landed-cost voucher — capitalises import-side charges (freight, customs duty, clearing &amp;
/// handling, insurance) onto a posted <see cref="GoodsReceiptNote"/>'s raw materials, raising
/// their weighted-average cost so the true landed cost flows into inventory valuation + COGS.
///
/// VALUE-ONLY: it changes cost, not quantity — no stock movement. Posting allocates the total
/// charges across the GRN lines (by value or by quantity), bumps each raw material's WAC by the
/// absorbed share ÷ current on-hand, and journals Dr Raw Material Inventory (+ Dr COGS for any
/// share that can't be absorbed because the stock was already consumed) / Cr Cash|Bank. Charges
/// are entered in base currency (BDT). Draft is editable; Posted is immutable.
/// </summary>
public class LandedCostVoucher : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly VoucherDate { get; set; }

    public long GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote GoodsReceiptNote { get; set; } = null!;

    public LandedCostAllocationBasis AllocationBasis { get; set; } = LandedCostAllocationBasis.ByValue;

    /// <summary>How the charges were settled — picks the credit account (Cash vs Bank). Ignored when <see cref="IsOnCredit"/>.</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    /// <summary>
    /// Phase A2 — when true, posting credits Accrued Charges Payable (2115) to the C&amp;F agent /
    /// <see cref="Supplier"/> instead of cash/bank, and the charge is cleared later via Settle.
    /// </summary>
    public bool IsOnCredit { get; set; }

    /// <summary>The agent/supplier owed the charges when <see cref="IsOnCredit"/>. Required in that case.</summary>
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public LandedCostVoucherStatus Status { get; set; } = LandedCostVoucherStatus.Draft;

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    // ── Settlement of an on-credit voucher (Phase A2) ──
    public DateOnly? SettledDate { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public string? SettledBy { get; set; }
    public PaymentMethod? SettlementMethod { get; set; }

    public string? Notes { get; set; }

    public ICollection<LandedCostCharge> Charges { get; set; } = new List<LandedCostCharge>();
}

/// <summary>One cost component on a <see cref="LandedCostVoucher"/> (e.g. ocean freight, customs duty).</summary>
public class LandedCostCharge : BaseTransactionalEntity
{
    public long LandedCostVoucherId { get; set; }
    public LandedCostVoucher LandedCostVoucher { get; set; } = null!;

    public LandedCostChargeType ChargeType { get; set; }

    /// <summary>Charge amount in base currency (BDT).</summary>
    public decimal Amount { get; set; }

    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public enum LandedCostAllocationBasis
{
    ByValue = 1,
    ByQuantity = 2
}

public enum LandedCostChargeType
{
    Freight = 1,
    CustomsDuty = 2,
    ClearingHandling = 3,
    Insurance = 4,
    Other = 99
}

public enum LandedCostVoucherStatus
{
    Draft = 1,
    Posted = 2
}
