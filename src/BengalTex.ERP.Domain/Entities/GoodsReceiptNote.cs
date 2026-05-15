using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Goods Receipt Note — records the physical arrival of materials against a
/// <see cref="PurchaseOrder"/>. Posting a GRN increments
/// <see cref="PurchaseOrderLine.ReceivedQuantity"/> on the matching PO lines and
/// drives the parent PO status to PartiallyReceived / Received.
///
/// Multiple GRNs may exist for a single PO (partial shipments over time).
/// Draft is editable; Posted is immutable for MVP — corrections handled by
/// separate inventory adjustments later.
/// </summary>
public class GoodsReceiptNote : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public long PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public DateOnly ReceiveDate { get; set; }

    /// <summary>Warehouse where the goods physically arrived.</summary>
    public int ReceivingWarehouseId { get; set; }
    public Warehouse ReceivingWarehouse { get; set; } = null!;

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

    /// <summary>Supplier's challan / delivery-note reference, if any.</summary>
    public string? SupplierDeliveryRef { get; set; }

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}

/// <summary>
/// A single line on a <see cref="GoodsReceiptNote"/>, recording the quantity received
/// against a specific <see cref="PurchaseOrderLine"/>.
/// </summary>
public class GoodsReceiptLine : BaseTransactionalEntity
{
    public long GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote GoodsReceiptNote { get; set; } = null!;

    public long PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;

    public decimal ReceivedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum GoodsReceiptStatus
{
    Draft = 1,
    Posted = 2
}
