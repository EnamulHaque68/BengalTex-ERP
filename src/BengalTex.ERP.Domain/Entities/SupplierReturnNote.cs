using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Supplier Return Note — records raw material physically returned to a supplier
/// against a previously posted <see cref="GoodsReceiptNote"/>. Posting an SRN:
///   1. Validates source warehouse has enough stock (two-pass atomic pattern);
///   2. Writes a <c>ReturnOut</c> RawMaterial stock movement at
///      <see cref="ReturnFromWarehouseId"/> (stock leaves our possession);
///   3. Increments <see cref="GoodsReceiptLine.ReturnedQuantity"/> — and via that,
///      decrements <see cref="PurchaseOrderLine.ReceivedQuantity"/> so the PO line
///      becomes "unfilled" by the returned qty.
///
/// PURELY INVENTORY for v1 — the linked Supplier Invoice's TotalAmount is NOT
/// touched. Finance handles any debit/refund via a separate Payment adjustment.
///
/// Lifecycle: Draft → Posted (immutable). No Cancel/Reverse — to "undo" a posted
/// SRN, post a counter-document.
/// Transactional (long key) — unbounded volume.
///
/// Mirror of <see cref="CustomerReturnNote"/> on the procurement side.
/// </summary>
public class SupplierReturnNote : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    /// <summary>The GRN this return is recorded against (defines which PO + supplier).</summary>
    public long GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote GoodsReceiptNote { get; set; } = null!;

    public DateOnly ReturnDate { get; set; }

    /// <summary>Warehouse the goods physically leave from (typically the RM store that received them).</summary>
    public int ReturnFromWarehouseId { get; set; }
    public Warehouse ReturnFromWarehouse { get; set; } = null!;

    public SupplierReturnNoteStatus Status { get; set; } = SupplierReturnNoteStatus.Draft;

    /// <summary>
    /// Phase A2 — set when the returned goods were received but NOT yet billed. Posting then
    /// credits the return against GR/IR Clearing (2150) — directly reversing the GRN's receipt
    /// liability — instead of Purchase Returns (5150), which is for billed goods. Keeps the
    /// GR/IR balance honest when goods go back before the supplier's bill arrives.
    /// </summary>
    public bool ClearsGrIr { get; set; }

    /// <summary>Vehicle / courier reference dispatching the return.</summary>
    public string? VehicleNumber { get; set; }

    /// <summary>Reason for return (defective, wrong specification, over-shipment, etc.).</summary>
    public string? Reason { get; set; }

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<SupplierReturnNoteLine> Lines { get; set; } = new List<SupplierReturnNoteLine>();
}

/// <summary>
/// A single line on a <see cref="SupplierReturnNote"/>, recording the quantity returned
/// against a specific <see cref="GoodsReceiptLine"/>. <see cref="RawMaterialId"/> is
/// denormalized off the GRN line chain for query simplicity.
/// </summary>
public class SupplierReturnNoteLine : BaseTransactionalEntity
{
    public long SupplierReturnNoteId { get; set; }
    public SupplierReturnNote SupplierReturnNote { get; set; } = null!;

    public long GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine GoodsReceiptLine { get; set; } = null!;

    /// <summary>Denormalized from GoodsReceiptLine.PurchaseOrderLine.RawMaterial.</summary>
    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public decimal ReturnedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum SupplierReturnNoteStatus
{
    Draft = 1,
    Posted = 2
}
