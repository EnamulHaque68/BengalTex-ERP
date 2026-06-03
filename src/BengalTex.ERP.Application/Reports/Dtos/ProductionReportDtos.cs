namespace BengalTex.ERP.Application.Reports.Dtos;

// ─── WIP (Work In Progress) Report ─────────────────────────────────────────
public record WipReportRowDto(
    long ProductionOrderId,
    string Code,
    int ProductId,
    string ProductCode,
    string ProductName,
    decimal TargetQuantity,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    int DaysRunning,                  // (today - ActualStartDate) in days; 0 if not started
    bool IsOverdue,                   // today > PlannedEndDate
    int TotalStages,
    int CompletedStages,
    decimal StageProgressPercent,     // CompletedStages / TotalStages * 100; 0 if no stages
    string? CurrentStageName,         // first non-Completed/Skipped stage
    string IssueWarehouseName,
    string ReceiveWarehouseName);

public record WipReportDto(
    DateOnly AsOfDate,
    int TotalOrdersInProgress,
    decimal TotalTargetQuantity,
    IReadOnlyList<WipReportRowDto> Rows);

// ─── Production Summary Report (completed in date range) ───────────────────
public record ProductionSummaryRowDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    int OrderCount,                   // number of orders completed in the window
    decimal TotalQuantityProduced,    // sum of completed-order qty
    decimal AverageQuantityPerOrder,
    decimal AverageCycleTimeDays);    // mean (ActualEndDate - ActualStartDate) — 0 if dates missing

public record ProductionSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalOrdersCompleted,
    decimal TotalQuantityProduced,
    IReadOnlyList<ProductionSummaryRowDto> Rows);
