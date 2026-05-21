using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Purchase Order — a procurement document raised against a <see cref="Supplier"/> for
/// raw materials. Lifecycle: Draft → Approved → Sent. Receiving (PartiallyReceived →
/// Received → Closed) is driven by GRN in Phase 4C. Cancellable from any pre-received
/// state. Transactional (long key) — unbounded volume.
/// </summary>
public class PurchaseOrder : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }

    /// <summary>Optional warehouse the goods are expected to be delivered to.</summary>
    public int? DeliveryWarehouseId { get; set; }
    public Warehouse? DeliveryWarehouse { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    /// <summary>Transaction currency (Phase 21). Line prices are expressed in this currency.</summary>
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    /// <summary>BDT per 1 unit of <see cref="Currency"/>, snapshot at transaction time. 1 for BDT.</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}

/// <summary>
/// A single raw-material line on a <see cref="PurchaseOrder"/>, expressed in the
/// raw material's own unit of measure.
/// </summary>
public class PurchaseOrderLine : BaseTransactionalEntity
{
    public long PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity received so far — updated by GRN postings in Phase 4C. Starts at 0.</summary>
    public decimal ReceivedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Approved = 2,
    Sent = 3,
    PartiallyReceived = 4,   // wired in Phase 4C (GRN-driven)
    Received = 5,            // wired in Phase 4C (GRN-driven)
    Closed = 6,              // wired in Phase 4C
    Cancelled = 7,
    PendingApproval = 8      // Phase 20 — awaiting approval-workflow sign-off (over threshold)
}
