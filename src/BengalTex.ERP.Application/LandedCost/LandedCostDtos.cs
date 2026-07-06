namespace BengalTex.ERP.Application.LandedCost;

public sealed record LandedCostChargeInput(string ChargeType, decimal Amount, string? Notes);

public sealed record LandedCostChargeDto(
    long Id, string ChargeType, decimal Amount, string? Notes, int SortOrder);

/// <summary>How the total charges land on one GRN line's raw material (preview shown in the detail view).</summary>
public sealed record LandedCostAllocationLineDto(
    int RawMaterialId, string RawMaterialCode, string RawMaterialName,
    decimal ReceivedQuantity, decimal LineValue, decimal AllocatedAmount, decimal AddedUnitCost);

public sealed record LandedCostVoucherDto(
    long Id, string Code, DateOnly VoucherDate,
    long GoodsReceiptNoteId, string GoodsReceiptCode, string PurchaseOrderCode, string SupplierName,
    string AllocationBasis, string PaymentMethod, string Status,
    DateTimeOffset? PostedAt, string? PostedBy, string? Notes,
    decimal TotalCharges,
    IReadOnlyList<LandedCostChargeDto> Charges,
    IReadOnlyList<LandedCostAllocationLineDto> Allocation,
    // Phase A2 — on-credit + settlement
    bool IsOnCredit = false, int? SupplierId = null, string? AgentSupplierName = null,
    DateOnly? SettledDate = null, string? SettledBy = null, string? SettlementMethod = null,
    bool IsSettled = false);

public sealed record LandedCostVoucherListItemDto(
    long Id, string Code, DateOnly VoucherDate, string GoodsReceiptCode, string SupplierName,
    string AllocationBasis, string Status, int ChargeCount, decimal TotalCharges,
    // Phase A2
    bool IsOnCredit = false, bool IsSettled = false);
