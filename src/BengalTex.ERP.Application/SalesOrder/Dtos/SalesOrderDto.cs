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
    string ProductionStatus = "NotStarted");

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
    decimal AllocatedQuantity = 0m);     // Σ qty of all non-cancelled production orders on this line

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
    string ProductionStatus = "NotStarted");
