namespace BengalTex.ERP.Application.Payment.Dtos;

public record PaymentDto(
    long Id,
    string Code,
    long SupplierInvoiceId,
    string SupplierInvoiceCode,
    int SupplierId,
    string SupplierName,
    DateOnly PaymentDate,
    decimal Amount,
    decimal ExchangeRate,                // BDT per 1 unit of invoice currency at payment time
    string PaymentMethod,                // enum as string
    string? ReferenceNumber,
    decimal AitAmount,                   // Phase A5b — BDT income tax withheld at source
    decimal VdsAmount,                   // Phase A5b — BDT VAT deducted at source
    string? Notes);

public record PaymentListItemDto(
    long Id,
    string Code,
    long SupplierInvoiceId,
    string SupplierInvoiceCode,
    int SupplierId,
    string SupplierName,
    DateOnly PaymentDate,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber);
