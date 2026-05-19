namespace BengalTex.ERP.Application.Reports.Dtos;

public record VatSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int CustomerInvoiceCount,
    decimal OutputVatNet,                  // sum of SubtotalAmount on issued customer invoices
    decimal OutputVatAmount,               // sum of VatAmount on issued customer invoices
    decimal OutputVatGross,                // sum of TotalAmount on issued customer invoices
    int SupplierInvoiceCount,
    decimal InputVatNet,                   // sum of SubtotalAmount on approved supplier invoices
    decimal InputVatAmount,                // sum of VatAmount on approved supplier invoices
    decimal InputVatGross,                 // sum of TotalAmount on approved supplier invoices
    decimal NetVatLiability,               // OutputVatAmount − InputVatAmount (positive = owe NBR)
    IReadOnlyList<VatSummaryMonthDto> Months);

public record VatSummaryMonthDto(
    int Year,
    int Month,
    string MonthLabel,                     // e.g. "May 2026"
    decimal OutputVatNet,
    decimal OutputVatAmount,
    decimal InputVatNet,
    decimal InputVatAmount,
    decimal NetVatLiability);
