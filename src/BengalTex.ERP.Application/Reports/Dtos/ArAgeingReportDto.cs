namespace BengalTex.ERP.Application.Reports.Dtos;

public record ArAgeingReportDto(
    DateOnly AsOfDate,
    int CustomerCount,
    int InvoiceCount,
    decimal TotalCurrent,
    decimal Total1To30,
    decimal Total31To60,
    decimal Total61To90,
    decimal Total90Plus,
    decimal TotalOutstanding,
    IReadOnlyList<ArAgeingCustomerDto> Customers);

public record ArAgeingCustomerDto(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus,
    decimal TotalOutstanding,
    int InvoiceCount,
    IReadOnlyList<ArAgeingInvoiceDto> Invoices);

public record ArAgeingInvoiceDto(
    long InvoiceId,
    string InvoiceCode,
    string SalesOrderCode,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    int DaysPastDue,                   // negative if not yet due
    string Bucket,                     // "Current" | "1-30" | "31-60" | "61-90" | "90+"
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue);
