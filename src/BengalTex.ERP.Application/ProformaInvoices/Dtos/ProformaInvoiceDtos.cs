namespace BengalTex.ERP.Application.ProformaInvoices.Dtos;

public sealed record ProformaInvoiceLineDto(
    long Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    string? ProductUnit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder,
    string? LineNotes);

public sealed record ProformaInvoiceDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerName,
    long? SalesOrderId,
    string? SalesOrderCode,
    DateOnly IssueDate,
    DateOnly ValidUntil,
    string Status,
    int CurrencyId,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal VatRate,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    DateTimeOffset? SentAt,
    string? SentBy,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? ExpiredAt,
    long? ConvertedCustomerInvoiceId,
    string? ConvertedCustomerInvoiceCode,
    string? Notes,
    IReadOnlyList<ProformaInvoiceLineDto> Lines);
