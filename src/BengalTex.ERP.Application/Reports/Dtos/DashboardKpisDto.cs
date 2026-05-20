namespace BengalTex.ERP.Application.Reports.Dtos;

public record DashboardKpisDto(
    DateTimeOffset GeneratedAt,
    int StockItemCount,                // distinct items currently holding stock (qty > 0)
    decimal TotalStockValue,           // Σ (qty × weighted-avg cost) across RM + Product (Phase 14)
    decimal OutstandingArAmount,       // sum of AmountDue across Issued + PartiallyPaid customer invoices
    int OutstandingArInvoiceCount,
    decimal OutstandingApAmount,       // sum of AmountDue across Approved + PartiallyPaid supplier invoices
    int OutstandingApInvoiceCount,
    decimal ThisMonthSalesAmount,      // sum of TotalAmount on non-Draft, non-Cancelled customer invoices issued this calendar month
    int ThisMonthSalesInvoiceCount,
    DateOnly MonthStart,
    DateOnly MonthEnd);
