namespace BengalTex.ERP.Application.Reports.Dtos;

public record ApAgeingReportDto(
    DateOnly AsOfDate,
    int SupplierCount,
    int InvoiceCount,
    decimal TotalCurrent,
    decimal Total1To30,
    decimal Total31To60,
    decimal Total61To90,
    decimal Total90Plus,
    decimal TotalOutstanding,
    IReadOnlyList<ApAgeingSupplierDto> Suppliers);

public record ApAgeingSupplierDto(
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus,
    decimal TotalOutstanding,
    int InvoiceCount,
    IReadOnlyList<ApAgeingInvoiceDto> Invoices);

public record ApAgeingInvoiceDto(
    long InvoiceId,
    string InvoiceCode,
    string PurchaseOrderCode,
    string? SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    int DaysPastDue,                   // negative if not yet due
    string Bucket,                     // "Current" | "1-30" | "31-60" | "61-90" | "90+"
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue);
