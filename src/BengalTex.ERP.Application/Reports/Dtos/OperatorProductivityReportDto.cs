namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>
/// Per-operator productivity rollup over the chosen date window. Aggregates every
/// <see cref="BengalTex.ERP.Domain.Entities.JobCard"/> on which the employee was the
/// assigned operator and whose status is Completed or InProgress (in-flight work
/// counts towards their throughput for the period). Cards from Cancelled production
/// orders are excluded.
/// </summary>
public record OperatorProductivityRowDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Department,
    string? Designation,
    int CompletedCards,
    int InProgressCards,
    decimal TotalCompletedQuantity,        // sum of CompletedQuantity across the cards
    decimal TotalRejectedQuantity,
    int TotalActiveMinutes,                // Σ ActiveMinutes (null cards counted as 0)
    decimal UnitsPerHour,                  // (TotalCompletedQuantity / TotalActiveMinutes) * 60; 0 if no active mins
    decimal RejectRatePercent,             // rejected / (completed + rejected) * 100; 0 if both 0
    decimal EfficiencyPercent);            // for now = pass-rate (1 - reject%) — placeholder for v1b w/ planned vs actual

public record OperatorProductivityReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalOperatorsActive,
    decimal GrandTotalCompletedQuantity,
    int GrandTotalActiveMinutes,
    decimal AverageUnitsPerHour,
    IReadOnlyList<OperatorProductivityRowDto> Rows);

/// <summary>
/// Per-machine productivity rollup — same shape, grouped by Machine instead of Operator.
/// Useful for capacity planning + maintenance impact analysis.
/// </summary>
public record MachineProductivityRowDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    string? MachineType,
    string? Location,
    int CompletedCards,
    int InProgressCards,
    decimal TotalCompletedQuantity,
    decimal TotalRejectedQuantity,
    int TotalActiveMinutes,
    decimal UnitsPerHour,
    decimal RejectRatePercent);

public record MachineProductivityReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalMachinesActive,
    decimal GrandTotalCompletedQuantity,
    int GrandTotalActiveMinutes,
    decimal AverageUnitsPerHour,
    IReadOnlyList<MachineProductivityRowDto> Rows);
