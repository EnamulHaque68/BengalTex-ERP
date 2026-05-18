using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Sales Order — a customer commitment to buy finished products. Lifecycle:
/// Draft → Confirmed → (PartiallyDispatched | Dispatched) → Closed; cancellable
/// from Draft or Confirmed. Dispatch transitions are driven by Delivery Note
/// postings (mirroring how GRN drives PO receipt transitions). Closure is a
/// manual decision (separates dispatch from final settlement, allows short-ship).
/// Transactional (long key) — unbounded volume.
/// </summary>
public class SalesOrder : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateOnly OrderDate { get; set; }
    public DateOnly? RequiredDeliveryDate { get; set; }

    /// <summary>The buyer's own PO/order reference number, if any.</summary>
    public string? CustomerPoRef { get; set; }

    /// <summary>Override delivery address — when goods ship somewhere other than the customer's HQ.</summary>
    public string? DeliveryAddress { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}

/// <summary>
/// A single line on a <see cref="SalesOrder"/>: a quantity of a finished <see cref="Product"/>.
/// </summary>
public class SalesOrderLine : BaseTransactionalEntity
{
    public long SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity dispatched so far — updated by Delivery Note postings. Starts at 0.</summary>
    public decimal DispatchedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum SalesOrderStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3,
    PartiallyDispatched = 4,    // DN-driven (some lines have DispatchedQuantity > 0 but not all complete)
    Dispatched = 5,             // DN-driven (all lines fully dispatched)
    Delivered = 6,              // future — customer-confirmed receipt (manual flag)
    Closed = 7                  // manual final settlement
}
