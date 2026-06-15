namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>
/// Standard-vs-actual wastage variance per completed production order. Standard wastage cost is
/// the BOM's built-in allowance (Σ line qty × wastage% × scale × RM weighted-avg cost); actual is
/// the sum of <c>WastageEntry</c> records linked to that order. Variance = Actual − Standard
/// (positive = wasted more than the BOM allowed). Surfaces where real waste exceeds the standard.
/// </summary>
public record WastageVarianceReportDto(
    DateTimeOffset GeneratedAt,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int? ProductId,
    int RowCount,
    decimal TotalStandardCost,
    decimal TotalActualCost,
    decimal TotalVariance,             // TotalActualCost − TotalStandardCost
    IReadOnlyList<WastageVarianceRowDto> Rows);

public record WastageVarianceRowDto(
    long ProductionOrderId,
    string ProductionOrderCode,
    DateOnly? CompletedDate,
    int ProductId,
    string ProductCode,
    string ProductName,
    decimal ProducedQuantity,
    decimal StandardWastageCost,       // BOM allowance × RM WAC
    decimal ActualWastageCost,         // Σ linked WastageEntry.TotalCost
    decimal Variance,                  // Actual − Standard
    decimal VariancePercent);          // Variance / Standard × 100 (0 when no standard)
