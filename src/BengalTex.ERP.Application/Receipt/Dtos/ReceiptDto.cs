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
    string CurrencyCode,                 // the invoice's currency (receipt's primary currency)
    decimal BaseAmount,                  // Amount × ExchangeRate, in company base currency (BDT)
    string Status,                       // Draft | Posted | Cancelled
    string PaymentMethod,                // enum as string
    string? ReferenceNumber,
    decimal BankChargeAmount,            // Phase A6b — FDBP bank commission (BDT)
    decimal InterestAmount,              // Phase A6b — FDBP interest/discount (BDT)
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
    string CurrencyCode,                 // the invoice's currency (receipt's primary currency)
    decimal ExchangeRate,                // BDT per 1 unit of CurrencyCode at receipt time
    decimal BaseAmount,                  // Amount × ExchangeRate, in base currency (BDT)
    string Status,                       // Draft | Posted | Cancelled
    string PaymentMethod,
    string? ReferenceNumber);
