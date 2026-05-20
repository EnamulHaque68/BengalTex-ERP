using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Customer Return Note — records goods physically returned by a customer against
/// a previously posted <see cref="DeliveryNote"/>. Posting a CRN:
///   1. Writes a <c>ReturnIn</c> Product stock movement at <see cref="ReturnWarehouseId"/>
///      (stock comes back into our possession);
///   2. Increments <see cref="DeliveryNoteLine.ReturnedQuantity"/> on each referenced
///      line — and via that, decrements <see cref="SalesOrderLine.DispatchedQuantity"/>
///      so the SO becomes "unfilled" by the returned qty (re-dispatchable).
///
/// PURELY INVENTORY for v1 — the linked Customer Invoice's TotalAmount is NOT
/// touched. Finance handles any credit/refund via a separate Receipt adjustment.
///
/// Lifecycle: Draft → Posted (immutable). No Cancel/Reverse — to "undo" a posted
/// CRN, post a counter-document.
/// Transactional (long key) — unbounded volume.
/// </summary>
public class CustomerReturnNote : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    /// <summary>The DN this return is recorded against (defines which SO + customer).</summary>
    public long DeliveryNoteId { get; set; }
    public DeliveryNote DeliveryNote { get; set; } = null!;

    public DateOnly ReturnDate { get; set; }

    /// <summary>Warehouse where returned goods are physically received back (typically FG store).</summary>
    public int ReturnWarehouseId { get; set; }
    public Warehouse ReturnWarehouse { get; set; } = null!;

    public CustomerReturnNoteStatus Status { get; set; } = CustomerReturnNoteStatus.Draft;

    /// <summary>Vehicle / courier reference that brought the goods back.</summary>
    public string? VehicleNumber { get; set; }

    /// <summary>Reason for return (defective, wrong size, customer rejection, etc.).</summary>
    public string? Reason { get; set; }

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<CustomerReturnNoteLine> Lines { get; set; } = new List<CustomerReturnNoteLine>();
}

/// <summary>
/// A single line on a <see cref="CustomerReturnNote"/>, recording the quantity returned
/// against a specific <see cref="DeliveryNoteLine"/>. <see cref="ProductId"/> is
/// denormalized off the DN line chain for query simplicity.
/// </summary>
public class CustomerReturnNoteLine : BaseTransactionalEntity
{
    public long CustomerReturnNoteId { get; set; }
    public CustomerReturnNote CustomerReturnNote { get; set; } = null!;

    public long DeliveryNoteLineId { get; set; }
    public DeliveryNoteLine DeliveryNoteLine { get; set; } = null!;

    /// <summary>Denormalized from DeliveryNoteLine.SalesOrderLine.Product — saves a 3-hop join in queries.</summary>
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal ReturnedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum CustomerReturnNoteStatus
{
    Draft = 1,
    Posted = 2
}
