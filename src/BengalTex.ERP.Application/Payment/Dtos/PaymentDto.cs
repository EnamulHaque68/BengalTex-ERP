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
    string PaymentMethod,                // enum as string
    string? ReferenceNumber,
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
