using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Permissions;

namespace BengalTex.ERP.Application.Attendance;

/// <summary>
/// Shared rules for who may review/approve attendance. A direct supervisor (the target's
/// ReportingToEmployeeId) can always act on their reports; HR/admins with org-wide rights "see all".
/// </summary>
public static class AttendanceSupervision
{
    /// <summary>True when the user reviews everyone (HR / admin / super-admin), not just direct reports.</summary>
    public static bool SeesAll(ICurrentUserService user) =>
        user.IsInRole("SuperAdmin") || user.IsInRole("Admin") ||
        user.HasPermission(Permissions.Attendance.ManualEntry) ||
        user.HasPermission(Permissions.Attendance.ViewSuspiciousActivity);
}
