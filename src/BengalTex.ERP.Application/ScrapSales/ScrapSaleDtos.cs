namespace BengalTex.ERP.Application.ScrapSales;

public sealed record ScrapSaleLineInput(string Description, decimal Quantity, string? Unit, decimal UnitPrice);

public sealed record ScrapSaleLineDto(
    long Id, string Description, decimal Quantity, string? Unit, decimal UnitPrice, decimal LineTotal, int SortOrder);

public sealed record ScrapSaleDto(
    long Id, string Code, DateOnly SaleDate, string? BuyerName, string PaymentMethod, string Status,
    DateTimeOffset? PostedAt, string? PostedBy, string? Notes, decimal TotalAmount,
    IReadOnlyList<ScrapSaleLineDto> Lines);

public sealed record ScrapSaleListItemDto(
    long Id, string Code, DateOnly SaleDate, string? BuyerName, string PaymentMethod, string Status,
    int LineCount, decimal TotalAmount);
