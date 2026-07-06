namespace BengalTex.ERP.Application.SalesOrder.Dtos;

public record SalesOrderDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly? RequiredDeliveryDate,
    string? CustomerPoRef,
    string? DeliveryAddress,
    string Status,                       // enum as string
    int CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal ExchangeRate,                // BDT per 1 unit of currency
    DateTimeOffset? ConfirmedAt,
    string? ConfirmedBy,
    string? Notes,
    decimal TotalAmount,                 // Σ line totals, in document currency
    decimal BaseTotalAmount,             // TotalAmount × ExchangeRate (BDT)
    IReadOnlyList<SalesOrderLineDto> Lines,
    // ── Phase 1: production progress (computed from linked production orders) ──
    decimal OrderedQuantity = 0m,        // Σ line quantities
    decimal ProducedQuantity = 0m,       // Σ completed production qty across lines
    decimal ProductionProgressPercent = 0m,
    string ProductionStatus = "NotStarted",
    // ── Sales A3: invoice coverage summary + traceability ──
    decimal InvoicedQuantity = 0m,       // Σ line invoiced quantities
    decimal InvoicedAmount = 0m,         // Σ (line.InvoicedQuantity × UnitPrice), document currency
    string InvoiceStatus = "NotInvoiced", // NotInvoiced | PartiallyInvoiced | FullyInvoiced
    IReadOnlyList<SalesOrderInvoiceRefDto>? RelatedInvoices = null);

/// <summary>A customer invoice raised against a sales order — for the SO details traceability list.</summary>
public record SalesOrderInvoiceRefDto(
    long Id,
    string Code,
    string Status,
    DateOnly InvoiceDate,
    decimal TotalAmount,
    decimal AmountPaid);

public record SalesOrderLineDto(
    long Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,                   // Quantity * UnitPrice
    int SortOrder,
    string? LineNotes,
    // ── Phase 1: production tracking per line ──
    decimal ProducedQuantity = 0m,       // Σ qty of Completed production orders on this line
    decimal AllocatedQuantity = 0m,      // Σ qty of all non-cancelled production orders on this line
    // ── Invoice coverage per line ──
    decimal InvoicedQuantity = 0m,       // Σ qty already billed onto customer invoices (remaining = Quantity − this)
    // ── Phase A3 — style dimension ──
    int? StyleId = null,
    string? StyleName = null);

public record SalesOrderListItemDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly? RequiredDeliveryDate,
    string Status,
    string CurrencyCode,
    decimal ExchangeRate,
    int LineCount,
    decimal TotalAmount,                 // in document currency
    decimal BaseTotalAmount,             // TotalAmount × ExchangeRate (BDT)
    // ── Phase 1: production progress ──
    decimal ProductionProgressPercent = 0m,
    string ProductionStatus = "NotStarted",
    // ── Invoice coverage (Sales A2) ──
    decimal OrderedQuantity = 0m,        // Σ line quantities
    decimal InvoicedQuantity = 0m,       // Σ line invoiced quantities
    string InvoiceStatus = "NotInvoiced"); // NotInvoiced | PartiallyInvoiced | FullyInvoiced
