namespace BengalTex.ERP.Application.CreditNotes.Dtos;

public sealed record CreditNoteDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerName,
    long CustomerInvoiceId,
    string CustomerInvoiceCode,
    decimal CustomerInvoiceTotal,
    decimal CustomerInvoiceAmountPaid,
    DateOnly IssueDate,
    string Reason,
    decimal Amount,
    int CurrencyId,
    string CurrencyCode,
    decimal ExchangeRate,
    string Status,
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    string? Notes);
