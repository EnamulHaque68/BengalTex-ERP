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
    decimal InvoicedNet,                   // sum of SubtotalAmount on issued invoices (Phase 12)
    decimal VatCollected,                  // sum of VatAmount on issued invoices (Phase 12)
    decimal InvoicedTotal,                 // gross — same as before Phase 12 (SubtotalAmount + VatAmount)
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
    decimal InvoicedNet,               // SubtotalAmount sum
    decimal VatCollected,              // VatAmount sum
    decimal InvoicedTotal,             // gross (SubtotalAmount + VatAmount sum)
    decimal AmountCollected,
    decimal AmountOutstanding);
