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
    int CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal ExchangeRate,                // BDT per 1 unit of currency (inherited from PO)
    decimal VatRate,                     // 0.15 = 15%
    decimal SubtotalAmount,              // net of VAT (document currency)
    decimal VatAmount,
    decimal TotalAmount,                 // SubtotalAmount + VatAmount (gross — what we owe)
    decimal AmountPaid,
    decimal AmountDue,                   // TotalAmount − AmountPaid
    decimal BaseTotalAmount,             // TotalAmount × ExchangeRate (BDT)
    DateTimeOffset? ApprovedAt,
    string? ApprovedBy,
    string? Notes,
    IReadOnlyList<SupplierInvoiceLineDto> Lines);

public record SupplierInvoiceLineDto(
    long Id,
    int? RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder,
    string? LineNotes,
    // Phase A2 — service line (mutually exclusive with the raw material)
    int? AccountId = null,
    string? AccountCode = null,
    string? AccountName = null,
    bool IsService = false);

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
    string CurrencyCode,
    decimal ExchangeRate,
    decimal VatRate,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue,
    decimal BaseTotalAmount,             // TotalAmount × ExchangeRate (BDT)
    int LineCount);
