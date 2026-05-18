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
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue,                   // TotalAmount − AmountPaid
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    string? Notes,
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
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue,
    int LineCount);
