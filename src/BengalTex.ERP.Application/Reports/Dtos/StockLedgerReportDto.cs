namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>
/// Per-item stock ledger (item ledger) — a running balance built from StockMovement rows
/// for one item (RawMaterial or Product), optionally a single warehouse, over a date range.
/// OpeningBalance = Σ signed qty strictly before <see cref="FromDate"/>; each row carries the
/// movement's in/out and the running balance after it; ClosingBalance = the last running balance.
/// </summary>
public record StockLedgerReportDto(
    DateTimeOffset GeneratedAt,
    string ItemType,                   // "RawMaterial" | "Product"
    int ItemId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    int? WarehouseId,                  // echo of filter (null = all warehouses)
    string? WarehouseName,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal OpeningBalance,
    decimal TotalIn,
    decimal TotalOut,
    decimal ClosingBalance,
    decimal UnitCost,                  // current weighted-average cost
    decimal ClosingValue,              // ClosingBalance × UnitCost
    IReadOnlyList<StockLedgerRowDto> Rows);

public record StockLedgerRowDto(
    DateOnly Date,
    string MovementCode,
    string MovementType,               // enum name, e.g. "GrnReceipt"
    string? ReferenceType,             // source doc kind, e.g. "GRN"
    string? ReferenceCode,             // source doc display code
    string WarehouseCode,
    string? LotNumber,
    decimal InQuantity,                // positive movements
    decimal OutQuantity,               // absolute of negative movements
    decimal RunningBalance,
    string? Notes);
