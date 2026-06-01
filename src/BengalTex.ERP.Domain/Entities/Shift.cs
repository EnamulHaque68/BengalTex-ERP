using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Work shift master (Day, Night, A/B, General). Carries the working hours + which day
/// of the week is the weekend for staff on this shift. v1a: master only.
/// v1b will integrate with WorkingDayCalculator (replacing hard-coded Friday).
/// </summary>
public class Shift : BaseEntity
{
    public string Code { get; set; } = string.Empty;     // "DAY", "NIGHT", "A", "B"
    public string Name { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Day of week treated as weekend for this shift (default Friday in BD).</summary>
    public DayOfWeek WeekendDayOfWeek { get; set; } = DayOfWeek.Friday;

    /// <summary>Optional second weekend day (some 5-day-week companies); v1a stores both for future use.</summary>
    public DayOfWeek? SecondWeekendDayOfWeek { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
