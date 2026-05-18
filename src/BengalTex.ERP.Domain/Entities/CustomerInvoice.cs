using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Customer Invoice — bill we raise to a <see cref="Customer"/> against a fulfilled
/// (Confirmed/Dispatched/Closed) <see cref="SalesOrder"/>. Lifecycle:
/// Draft → Issued → (PartiallyPaid → Paid); cancellable from Draft or Issued as long
/// as no <see cref="Receipt"/>s have been applied. Once Issued, lines are immutable;
/// only payment state changes via Receipts.
///
/// <see cref="TotalAmount"/> is a snapshot (sum of line totals, computed on Save).
/// <see cref="AmountPaid"/> is a denormalized running sum maintained by Receipt
/// create/delete — keeps status recompute O(1).
/// Transactional (long key) — unbounded volume.
/// </summary>
public class CustomerInvoice : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public long SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    /// <summary>Default = InvoiceDate + Customer.PaymentTermsDays; editable.</summary>
    public DateOnly DueDate { get; set; }

    public CustomerInvoiceStatus Status { get; set; } = CustomerInvoiceStatus.Draft;

    /// <summary>Sum of line totals — written on Save, locked at Issue.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Running sum of all non-deleted <see cref="Receipt"/>s applied. Updated atomically.</summary>
    public decimal AmountPaid { get; set; }

    public DateTimeOffset? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<CustomerInvoiceLine> Lines { get; set; } = new List<CustomerInvoiceLine>();
}

/// <summary>
/// A single product line on a <see cref="CustomerInvoice"/>. Editable while the parent is
/// in Draft state; locked once Issued.
/// </summary>
public class CustomerInvoiceLine : BaseTransactionalEntity
{
    public long CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum CustomerInvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,    // 0 < AmountPaid < TotalAmount
    Paid = 4,             // AmountPaid >= TotalAmount
    Cancelled = 5         // only allowed when AmountPaid = 0
}
