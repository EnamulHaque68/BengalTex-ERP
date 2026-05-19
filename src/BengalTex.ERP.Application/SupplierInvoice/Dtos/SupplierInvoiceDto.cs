namespace BengalTex.ERP.Application.SupplierInvoice.Dtos;

public record SupplierInvoiceDto(
    long Id,
    string Code,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    long PurchaseOrderId,
    string PurchaseOrderCode,
    string? SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string Status,
    decimal VatRate,                     // 0.15 = 15%
    decimal SubtotalAmount,              // net of VAT
    decimal VatAmount,
    decimal TotalAmount,                 // SubtotalAmount + VatAmount (gross — what we owe)
    decimal AmountPaid,
    decimal AmountDue,                   // TotalAmount − AmountPaid
    DateTimeOffset? ApprovedAt,
    string? ApprovedBy,
    string? Notes,
    IReadOnlyList<SupplierInvoiceLineDto> Lines);

public record SupplierInvoiceLineDto(
    long Id,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder,
    string? LineNotes);

public record SupplierInvoiceListItemDto(
    long Id,
    string Code,
    int SupplierId,
    string SupplierName,
    long PurchaseOrderId,
    string PurchaseOrderCode,
    string? SupplierInvoiceNumber,
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
