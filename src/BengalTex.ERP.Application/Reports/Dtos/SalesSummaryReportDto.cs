namespace BengalTex.ERP.Application.Reports.Dtos;

public record SalesSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int? CustomerId,
    string? CustomerName,
    int CustomerCount,
    int SalesOrderCount,
    decimal SalesOrderTotal,
    int DeliveryNoteCount,
    decimal DeliveryNoteValue,
    int InvoiceCount,
    decimal InvoicedTotal,
    decimal AmountCollected,
    decimal AmountOutstanding,
    IReadOnlyList<SalesSummaryRowDto> Rows);

public record SalesSummaryRowDto(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int SalesOrderCount,
    decimal SalesOrderTotal,
    int DeliveryNoteCount,
    decimal DeliveryNoteValue,         // sum of (line.dispatchedQty * SO line.unitPrice) — approximate
    int InvoiceCount,
    decimal InvoicedTotal,
    decimal AmountCollected,
    decimal AmountOutstanding);
