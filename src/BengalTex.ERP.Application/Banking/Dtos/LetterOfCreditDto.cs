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
    string? Notes);

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
