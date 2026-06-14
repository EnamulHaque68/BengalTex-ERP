namespace BengalTex.ERP.Application.Receipt.Dtos;

public record ReceiptDto(
    long Id,
    string Code,
    long CustomerInvoiceId,
    string CustomerInvoiceCode,
    int CustomerId,
    string CustomerName,
    DateOnly ReceiptDate,
    decimal Amount,
    decimal ExchangeRate,                // BDT per 1 unit of invoice currency at receipt time
    string PaymentMethod,                // enum as string
    string? ReferenceNumber,
    string? Notes);

public record ReceiptListItemDto(
    long Id,
    string Code,
    long CustomerInvoiceId,
    string CustomerInvoiceCode,
    int CustomerId,
    string CustomerName,
    DateOnly ReceiptDate,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber);
