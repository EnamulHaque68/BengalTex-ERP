using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Attendance;

/// <summary>Effective attendance policy (from AttendanceSettings, or sensible defaults when unset).</summary>
public sealed record AttendancePolicyValues(
    TimeOnly OfficeStart,
    TimeOnly OfficeEnd,
    int GraceMinutes,
    OutsideFenceMode OutsideFenceMode,
    bool RequireSelfie,
    bool RequireSupervisorApproval)
{
    public static AttendancePolicyValues From(AttendanceSettings? s) => s is null
        ? new AttendancePolicyValues(new(9, 0), new(18, 0), 15, OutsideFenceMode.Flag, false, false)
        : new AttendancePolicyValues(s.OfficeStartTime, s.OfficeEndTime, s.GracePeriodMinutes,
            s.OutsideFenceMode, s.RequireSelfie, s.RequireSupervisorApproval);
}

/// <summary>Pure office-time classification — no DB, no side effects.</summary>
public static class AttendancePolicy
{
    /// <summary>On-time if check-in is within (start + grace); otherwise Late.</summary>
    public static (AttendanceStatus status, bool isLate) ClassifyCheckIn(TimeOnly checkIn, AttendancePolicyValues p)
    {
        var cutoff = p.OfficeStart.AddMinutes(p.GraceMinutes);
        return checkIn <= cutoff
            ? (AttendanceStatus.OnTime, false)
            : (AttendanceStatus.Late, true);
    }

    /// <summary>
    /// Worked minutes = (check-out − check-in) − break minutes. Early-leave if before office end;
    /// overtime hours = minutes worked past office end (≥0).
    /// </summary>
    public static (int workedMinutes, bool isEarlyLeave, decimal overtimeHours) ClassifyCheckOut(
        TimeOnly checkIn, TimeOnly checkOut, int breakMinutes, AttendancePolicyValues p)
    {
        var gross = (int)(checkOut - checkIn).TotalMinutes;
        var worked = Math.Max(0, gross - Math.Max(0, breakMinutes));
        var isEarlyLeave = checkOut < p.OfficeEnd;
        var otMinutes = checkOut > p.OfficeEnd ? (int)(checkOut - p.OfficeEnd).TotalMinutes : 0;
        return (worked, isEarlyLeave, Math.Round(otMinutes / 60m, 2));
    }
}
