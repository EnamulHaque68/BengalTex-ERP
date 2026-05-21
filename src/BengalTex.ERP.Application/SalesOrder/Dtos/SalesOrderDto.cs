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
    IReadOnlyList<SalesOrderLineDto> Lines);

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
    string? LineNotes);

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
    decimal BaseTotalAmount);            // TotalAmount × ExchangeRate (BDT)
