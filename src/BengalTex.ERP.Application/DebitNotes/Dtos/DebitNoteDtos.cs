namespace BengalTex.ERP.Application.DebitNotes.Dtos;

public sealed record DebitNoteDto(
    long Id,
    string Code,
    int SupplierId,
    string SupplierName,
    long SupplierInvoiceId,
    string SupplierInvoiceCode,
    decimal SupplierInvoiceTotal,
    decimal SupplierInvoiceAmountPaid,
    DateOnly IssueDate,
    string Reason,
    decimal Amount,
    int CurrencyId,
    string CurrencyCode,
    decimal ExchangeRate,
    string Status,
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    string? Notes,
    long? SupplierReturnNoteId = null,
    string? SupplierReturnNoteCode = null);
