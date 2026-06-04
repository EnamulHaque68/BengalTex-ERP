namespace BengalTex.ERP.Application.FixedAssets.Dtos;

public sealed record FixedAssetDto(
    long Id,
    string Code,
    string Name,
    string Category,
    string? Location,
    int? MachineId,
    string? MachineCode,
    DateOnly AcquisitionDate,
    decimal AcquisitionCost,
    decimal SalvageValue,
    int UsefulLifeYears,
    string DepreciationMethod,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    decimal MonthlyDepreciation,           // computed
    int? LastDepreciationYearMonth,
    string Status,
    DateOnly? DisposalDate,
    decimal? DisposalProceeds,
    string? DisposalNotes,
    string? DisposedByUser,
    string? Notes);

public sealed record AssetDepreciationRunLineDto(
    long Id,
    long FixedAssetId,
    string FixedAssetCode,
    string FixedAssetName,
    decimal MonthlyDepreciation,
    decimal AccumulatedAfter,
    decimal NetBookValueAfter);

public sealed record AssetDepreciationRunDto(
    long Id,
    string Code,
    int Year,
    int Month,
    DateOnly RunDate,
    decimal TotalAmount,
    int AssetCount,
    string? PostedByUser,
    string? Notes,
    IReadOnlyList<AssetDepreciationRunLineDto> Lines);
