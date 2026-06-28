namespace BengalTex.ERP.Application.Production.Dtos;

/// <summary>
/// Read-only Manufacturing Calendar payload for a date range — production orders scheduled in the
/// window plus the non-working-day context (holidays + weekly off-days) so the UI can shade days.
/// Pure projection over existing entities (ProductionOrder, Holiday, Shift) — no new entity, no
/// migration, consistent with the "Planning = VIEW" decision.
/// </summary>
public record ProductionCalendarDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<int> WeekendDays,                          // DayOfWeek ints (0=Sunday … 6=Saturday)
    IReadOnlyList<ProductionCalendarHolidayDto> Holidays,
    IReadOnlyList<ProductionCalendarEventDto> Orders);

public record ProductionCalendarHolidayDto(DateOnly Date, string Name);

/// <summary>One production order plotted on the calendar by its planned (or actual) span.</summary>
public record ProductionCalendarEventDto(
    long Id,
    string Code,
    int ProductId,
    string ProductName,
    decimal Quantity,
    string Status,                                           // Draft | InProgress | Completed | Cancelled
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    long? SalesOrderId,
    string? SalesOrderCode);
