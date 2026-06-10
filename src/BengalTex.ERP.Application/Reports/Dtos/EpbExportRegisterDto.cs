namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>
/// One row on the EPB Export Register — a foreign-currency (non-BDT) CustomerInvoice
/// that's been Issued (not Draft / not Cancelled). All the fields BD's Export Promotion
/// Bureau Form-N needs in one row. ShipmentDate / EpbFormNumber / LcNumber may be null
/// until the bank issues Form-EXP and the user records it via Mark-as-Exported.
/// </summary>
public record EpbExportRegisterRowDto(
    long InvoiceId,
    string InvoiceCode,
    DateOnly InvoiceDate,
    DateOnly? ShipmentDate,
    string? EpbFormNumber,
    string? LcNumber,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    string CountryOfDestination,                  // = Customer.Country
    string SalesOrderCode,
    string CurrencyCode,
    decimal ExchangeRate,                          // BDT per 1 unit of foreign currency
    decimal FobAmountForeign,                      // subtotal (FOB; pre-VAT) in FX
    decimal FobAmountBdt,                          // = subtotal × rate
    decimal TotalAmountForeign,                    // gross including VAT
    decimal TotalAmountBdt,
    string Status,
    string? HsCodesSummary);                       // distinct HS codes on the invoice's lines, comma-separated

public record EpbExportRegisterReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalInvoices,
    int InvoicesPendingFormExp,                    // EpbFormNumber == null
    decimal GrandFobBdt,
    decimal GrandTotalBdt,
    IReadOnlyList<EpbExportRegisterRowDto> Rows);
