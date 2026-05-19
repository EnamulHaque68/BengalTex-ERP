namespace BengalTex.ERP.Application.VatChallan.Dtos;

public record VatChallanDto(
    long Id,
    string Code,
    long CustomerInvoiceId,
    string CustomerInvoiceCode,
    int CustomerId,
    string CustomerName,
    DateOnly ChallanDate,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    string? Notes);

public record VatChallanListItemDto(
    long Id,
    string Code,
    long CustomerInvoiceId,
    string CustomerInvoiceCode,
    int CustomerId,
    string CustomerName,
    DateOnly ChallanDate,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount);
