namespace BengalTex.ERP.Application.SupplierQuotations;

public sealed record SupplierQuotationLineInput(
    int RawMaterialId, decimal Quantity, decimal UnitPrice, int? LeadTimeDays, string? LineNotes);

public sealed record SupplierQuotationLineDto(
    long Id, int RawMaterialId, string RawMaterialCode, string RawMaterialName, string? RawMaterialUnit,
    decimal Quantity, decimal UnitPrice, decimal LineTotal, int? LeadTimeDays, int SortOrder, string? LineNotes);

public sealed record SupplierQuotationDto(
    long Id, string Code, DateOnly QuotationDate,
    int SupplierId, string SupplierName,
    long? PurchaseRequisitionId, string? PurchaseRequisitionCode,
    int CurrencyId, string CurrencyCode, decimal ExchangeRate,
    DateOnly? ValidUntil, string Status,
    DateTimeOffset? DecidedAt, string? DecidedBy,
    long? ConvertedPurchaseOrderId, DateTimeOffset? ConvertedAt,
    string? Notes, decimal TotalAmount, decimal TotalAmountBase,
    IReadOnlyList<SupplierQuotationLineDto> Lines);

public sealed record SupplierQuotationListItemDto(
    long Id, string Code, DateOnly QuotationDate, string SupplierName,
    string? PurchaseRequisitionCode, string CurrencyCode, string Status,
    int LineCount, decimal TotalAmount, decimal TotalAmountBase);

// ── Comparison matrix (by purchase requisition) ──
public sealed record QuotationComparisonSupplierDto(
    long SupplierQuotationId, string Code, string SupplierName, string CurrencyCode, decimal ExchangeRate,
    string Status, DateOnly? ValidUntil, decimal TotalBase, bool IsLowestTotal);

public sealed record QuotationComparisonCellDto(
    long SupplierQuotationId, bool HasQuote, decimal UnitPrice, decimal UnitPriceBase,
    int? LeadTimeDays, decimal LineTotalBase, bool IsLowest);

public sealed record QuotationComparisonRowDto(
    int RawMaterialId, string RawMaterialCode, string RawMaterialName, decimal Quantity,
    IReadOnlyList<QuotationComparisonCellDto> Cells);

public sealed record QuotationComparisonDto(
    long PurchaseRequisitionId, string PurchaseRequisitionCode,
    IReadOnlyList<QuotationComparisonSupplierDto> Suppliers,
    IReadOnlyList<QuotationComparisonRowDto> Rows);
