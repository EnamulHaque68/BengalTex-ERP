namespace BengalTex.ERP.Application.MachineMaintenance.Dtos;

public sealed record MachineMaintenanceDto(
    long Id,
    string Code,
    int MachineId,
    string MachineCode,
    string MachineName,
    string? MachineType,
    string? MachineLocation,
    string Type,
    string Description,
    DateOnly ScheduledDate,
    DateOnly? CompletedDate,
    decimal? DowntimeHours,
    string? PerformedBy,
    int? PerformedByEmployeeId,
    string? PerformedByEmployeeName,
    decimal ServiceCost,
    decimal PartsCost,
    decimal TotalCost,
    string? PartsReplaced,
    string? CompletionNotes,
    string Status,
    bool IsOverdue,                  // computed: Scheduled && today > ScheduledDate
    bool IsRecurring,
    int? IntervalDays,
    long? RecurringSeriesAnchorId,
    string? Notes);
