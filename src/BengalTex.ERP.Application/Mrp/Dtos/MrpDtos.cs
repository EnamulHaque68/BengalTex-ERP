namespace BengalTex.ERP.Application.Mrp.Dtos;

/// <summary>
/// One raw material's material-requirement line (Phase 3 — MRP). Demand ("Required") is the
/// firm production plan: the sum of Active reservations across warehouses (every open Production
/// Order reserves its BOM raw materials in Phase 2), so Required ≡ Reserved. Supply = on-hand
/// physical stock + incoming (open Purchase Orders not yet received). Shortage is what must be
/// bought to cover the plan:  Shortage = max(0, Required − OnHand − Incoming).
/// </summary>
public record MrpItemDto(
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal RequiredQuantity,        // = reserved (firm open-production demand)
    decimal OnHandQuantity,          // physical stock, all warehouses
    decimal AvailableQuantity,       // OnHand − Required(Reserved)
    decimal IncomingQuantity,        // open Purchase Orders, qty ordered − received
    decimal ShortageQuantity,        // max(0, Required − OnHand − Incoming)
    decimal EstimatedUnitPrice,      // raw material weighted-average cost (for the PR estimate)
    decimal MinimumStockLevel);

/// <summary>The full MRP run — net-requirement rows plus a quick summary.</summary>
public record MrpResultDto(
    IReadOnlyList<MrpItemDto> Items,
    int ShortageCount,                       // rows with ShortageQuantity > 0
    decimal TotalEstimatedShortageCost);     // Σ shortage × estimated unit price
