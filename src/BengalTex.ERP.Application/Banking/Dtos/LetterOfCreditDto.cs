namespace BengalTex.ERP.Application.Banking.Dtos;

public sealed record LetterOfCreditDto(
    long Id,
    string Code,
    string LcNumber,
    string IssuingBank,
    int SupplierId,
    string SupplierName,
    long? PurchaseOrderId,
    string? PurchaseOrderCode,
    int CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal ExchangeRate,
    decimal Amount,
    decimal BaseAmount,            // Amount × ExchangeRate (BDT)
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    int TenorDays,
    string Status,
    string Type,                   // "Import" | "BackToBack"
    string? MasterLcReference,
    string? MasterLcBuyer,
    DateOnly? ShipmentDate,
    DateOnly? SettlementDate,
    string? Notes,
    // ── Goods-receipt utilisation summary (Area B); LC-currency values assume PO currency = LC currency ──
    decimal ReceivedAmount = 0m,         // Σ posted-GRN received value
    decimal RemainingAmount = 0m,        // Amount − ReceivedAmount
    decimal ReceivedQuantity = 0m,       // Σ posted-GRN received qty
    decimal OrderedQuantity = 0m,        // linked PO ordered qty (Remaining = Ordered − Received)
    decimal UtilizationPercent = 0m,     // ReceivedAmount ÷ Amount × 100
    IReadOnlyList<LcGoodsReceiptRefDto>? RelatedGoodsReceipts = null);

/// <summary>A goods receipt drawn against a letter of credit — for the LC details traceability list.</summary>
public sealed record LcGoodsReceiptRefDto(
    long Id,
    string Code,
    string Status,
    DateOnly ReceiveDate,
    decimal ReceivedQuantity,
    decimal ReceivedAmount);

public sealed record LetterOfCreditListItemDto(
    long Id,
    string Code,
    string LcNumber,
    string IssuingBank,
    string SupplierName,
    string CurrencyCode,
    decimal Amount,
    decimal BaseAmount,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string Status,
    string Type);
