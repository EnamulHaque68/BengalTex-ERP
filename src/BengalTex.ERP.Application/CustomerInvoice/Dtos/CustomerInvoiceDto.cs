namespace BengalTex.ERP.Application.CustomerInvoice.Dtos;

public record CustomerInvoiceDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    long SalesOrderId,
    string SalesOrderCode,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string Status,                       // enum as string
    int CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal ExchangeRate,                // BDT per 1 unit of currency (inherited from SO)
    decimal VatRate,                     // 0.15 = 15%
    decimal SubtotalAmount,              // sum of line totals, net of VAT (document currency)
    decimal VatAmount,                   // SubtotalAmount × VatRate
    decimal TotalAmount,                 // SubtotalAmount + VatAmount (gross — what's owed)
    decimal AmountPaid,
    decimal AmountDue,                   // TotalAmount − AmountPaid
    decimal BaseTotalAmount,             // TotalAmount × ExchangeRate (BDT)
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    string? Notes,
    string? VatChallanCode,              // populated when challan auto-issued
    // BD export reporting (foreign-currency invoices)
    string? EpbFormNumber,
    string? LcNumber,
    DateOnly? ShipmentDate,
    // Export shipping document fields (Commercial Invoice / Packing List)
    string? IncoTerm,
    string? PortOfLoading,
    string? PortOfDischarge,
    string? VesselName,
    string? CountryOfDestination,
    string? ShippingMarks,
    int? TotalPackages,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    IReadOnlyList<CustomerInvoiceLineDto> Lines);

public record CustomerInvoiceLineDto(
    long Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    string? HsCode,                      // for export Commercial Invoice / Packing List
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,                   // Quantity × UnitPrice
    int SortOrder,
    string? LineNotes,
    // Per-line export packing breakdown (Packing List)
    int? CartonNumberFrom,
    int? CartonNumberTo,
    int? UnitsPerCarton,
    decimal? NetWeightKgPerLine,
    decimal? GrossWeightKgPerLine);

public record CustomerInvoiceListItemDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerName,
    long SalesOrderId,
    string SalesOrderCode,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string Status,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal VatRate,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue,
    decimal BaseTotalAmount,             // TotalAmount × ExchangeRate (BDT)
    int LineCount,
    // BD export reporting
    string? EpbFormNumber,
    DateOnly? ShipmentDate,
    bool CustomerIsExport);
