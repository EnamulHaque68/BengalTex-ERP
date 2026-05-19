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
    decimal VatRate,                     // 0.15 = 15%
    decimal SubtotalAmount,              // sum of line totals, net of VAT
    decimal VatAmount,                   // SubtotalAmount × VatRate
    decimal TotalAmount,                 // SubtotalAmount + VatAmount (gross — what's owed)
    decimal AmountPaid,
    decimal AmountDue,                   // TotalAmount − AmountPaid
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    string? Notes,
    string? VatChallanCode,              // populated when challan auto-issued
    IReadOnlyList<CustomerInvoiceLineDto> Lines);

public record CustomerInvoiceLineDto(
    long Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,                   // Quantity × UnitPrice
    int SortOrder,
    string? LineNotes);

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
    decimal VatRate,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue,
    int LineCount);
