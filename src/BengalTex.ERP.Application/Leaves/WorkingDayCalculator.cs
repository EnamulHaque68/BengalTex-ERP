namespace BengalTex.ERP.Application.Leaves;

/// <summary>
/// Computes inclusive "working days" between two dates, excluding the configured
/// weekend day (Bangladesh standard: Friday) and any active holiday dates.
/// v1: weekend hard-coded to Friday; v2 will read from a Shift/CompanySetting record.
/// </summary>
public static class WorkingDayCalculator
{
    public const DayOfWeek Weekend = DayOfWeek.Friday;

    public static decimal CountWorkingDays(DateOnly from, DateOnly to, IReadOnlySet<DateOnly> activeHolidays)
    {
        if (to < from) return 0;
        int count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (d.DayOfWeek == Weekend) continue;
            if (activeHolidays.Contains(d)) continue;
            count++;
        }
        return count;
    }

    public static IEnumerable<DateOnly> EnumerateWorkingDays(DateOnly from, DateOnly to, IReadOnlySet<DateOnly> activeHolidays)
    {
        if (to < from) yield break;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (d.DayOfWeek == Weekend) continue;
            if (activeHolidays.Contains(d)) continue;
            yield return d;
        }
    }
}
