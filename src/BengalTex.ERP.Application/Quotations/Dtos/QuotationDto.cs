namespace BengalTex.ERP.Application.Quotations.Dtos;

public record QuotationDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerName,
    DateOnly QuotationDate,
    DateOnly? ValidUntil,
    int CurrencyId,
    string CurrencyCode,
    decimal ExchangeRate,
    string Status,
    int Version,
    long? RevisionOfId,
    decimal TotalAmount,
    string? CustomerReference,
    string? Notes,
    DateTimeOffset? SentAt,
    DateTimeOffset? DecidedAt,
    string? DecidedBy,
    long? ConvertedSalesOrderId,
    IReadOnlyList<QuotationLineDto> Lines);

public record QuotationLineDto(
    long Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    string? Description,
    decimal Quantity,
    decimal MaterialCost,
    decimal LaborCost,
    decimal MachineCost,
    decimal OverheadCost,
    decimal WastagePercent,
    decimal MarginPercent,
    decimal UnitCost,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder);

public record QuotationListItemDto(
    long Id,
    string Code,
    string CustomerName,
    DateOnly QuotationDate,
    DateOnly? ValidUntil,
    string CurrencyCode,
    decimal TotalAmount,
    string Status,
    int Version,
    int LineCount);
