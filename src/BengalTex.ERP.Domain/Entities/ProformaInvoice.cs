using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Proforma Invoice — a NON-BINDING quote/invoice we send to a customer before goods
/// are delivered. Common in export trade for advance payment / LC opening. Does NOT
/// hit AR, does NOT post a ledger entry, does NOT auto-issue a VAT challan. Once goods
/// ship and a real <see cref="CustomerInvoice"/> is raised, the Proforma can be marked
/// Converted (1-to-1 link via <see cref="ConvertedCustomerInvoiceId"/>).
///
/// Lifecycle: Draft → Sent → Accepted | Expired | Cancelled. Lines editable while Draft.
/// </summary>
public class ProformaInvoice : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;   // "PFM-####"

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Optional source SO. Proforma can stand alone or sit between Quotation and CustomerInvoice.</summary>
    public long? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>Optional source Quotation — set when this Proforma was generated from a quotation
    /// (pre-order flow: Quotation → Proforma → Sales Order). One active proforma per quotation.</summary>
    public long? QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public DateOnly IssueDate { get; set; }

    /// <summary>Quote validity. After this date the Proforma can be marked Expired.</summary>
    public DateOnly ValidUntil { get; set; }

    public ProformaInvoiceStatus Status { get; set; } = ProformaInvoiceStatus.Draft;

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public decimal VatRate { get; set; }              // 0.15 for BD 15%
    public decimal SubtotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }          // gross — what we're quoting

    public DateTimeOffset? SentAt { get; set; }
    public string? SentBy { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }

    /// <summary>FK populated when this Proforma is converted into a real CustomerInvoice.</summary>
    public long? ConvertedCustomerInvoiceId { get; set; }
    public CustomerInvoice? ConvertedCustomerInvoice { get; set; }

    /// <summary>FK populated when a Sales Order is created from this Proforma (after customer confirmation).</summary>
    public long? ConvertedSalesOrderId { get; set; }

    // ── Customer confirmation (how the customer confirmed the order against this proforma) ──
    /// <summary>PurchaseOrder | LetterOfCredit | AdvancePayment | SignedProforma | EmailApproval.</summary>
    public string? CustomerConfirmationType { get; set; }
    /// <summary>Reference no — PO number, LC number, payment ref, etc.</summary>
    public string? CustomerConfirmationReference { get; set; }
    public DateOnly? CustomerConfirmationDate { get; set; }
    /// <summary>Storage path of the supporting document (PO PDF / LC copy / signed proforma / email screenshot).</summary>
    public string? CustomerConfirmationAttachment { get; set; }

    public string? Notes { get; set; }

    public ICollection<ProformaInvoiceLine> Lines { get; set; } = new List<ProformaInvoiceLine>();
}

public class ProformaInvoiceLine : BaseTransactionalEntity
{
    public long ProformaInvoiceId { get; set; }
    public ProformaInvoice ProformaInvoice { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum ProformaInvoiceStatus
{
    Draft = 1,
    Sent = 2,
    Accepted = 3,
    Expired = 4,
    Cancelled = 5,
    Converted = 6   // a real CustomerInvoice was raised from this proforma
}
