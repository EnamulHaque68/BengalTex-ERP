using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Delivery Note — records the physical dispatch of finished products against a
/// <see cref="SalesOrder"/>. Posting a DN increments
/// <see cref="SalesOrderLine.DispatchedQuantity"/> on the matching SO lines and
/// drives the parent SO status to PartiallyDispatched / Dispatched.
///
/// Multiple DNs may exist for a single SO (partial dispatches over time).
/// Draft is editable; Posted is immutable for MVP — corrections handled by
/// separate Return Note / inventory adjustments later.
///
/// Symmetric counterpart of <see cref="GoodsReceiptNote"/> on the sales side.
/// </summary>
public class DeliveryNote : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public long SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public DateOnly DispatchDate { get; set; }

    /// <summary>Warehouse the goods physically dispatched from (typically FG store).</summary>
    public int DispatchWarehouseId { get; set; }
    public Warehouse DispatchWarehouse { get; set; } = null!;

    public DeliveryNoteStatus Status { get; set; } = DeliveryNoteStatus.Draft;

    /// <summary>Vehicle / courier reference (truck number, courier tracking, etc.).</summary>
    public string? VehicleNumber { get; set; }

    /// <summary>Driver / courier contact.</summary>
    public string? DriverContact { get; set; }

    /// <summary>Override delivery address (defaults to SO.DeliveryAddress or Customer's HQ).</summary>
    public string? DeliveryAddress { get; set; }

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<DeliveryNoteLine> Lines { get; set; } = new List<DeliveryNoteLine>();
}

/// <summary>
/// A single line on a <see cref="DeliveryNote"/>, recording the quantity dispatched
/// against a specific <see cref="SalesOrderLine"/>.
/// </summary>
public class DeliveryNoteLine : BaseTransactionalEntity
{
    public long DeliveryNoteId { get; set; }
    public DeliveryNote DeliveryNote { get; set; } = null!;

    public long SalesOrderLineId { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; } = null!;

    public decimal DispatchedQuantity { get; set; }

    /// <summary>
    /// Running sum of quantity returned by customers against this line (via posted
    /// <c>CustomerReturnNote</c>s). Updated atomically when a CRN is posted.
    /// Available qty for further returns = DispatchedQuantity − ReturnedQuantity.
    /// </summary>
    public decimal ReturnedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum DeliveryNoteStatus
{
    Draft = 1,
    Posted = 2
}
