using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Attendance;

/// <summary>A single red-flag on an attendance row, for the supervisor review view.</summary>
public sealed record AttendanceFlag(string Code, string Label, string Severity);  // severity: critical | warning | info

/// <summary>
/// Pure derivation of "red flags" from an attendance row — buddy-punch / spoofing / discipline signals
/// the supervisor should see. No DB, no side effects; computed from already-captured fields.
/// </summary>
public static class AttendanceFlags
{
    public static IReadOnlyList<AttendanceFlag> For(
        bool? withinFence, bool? isProxyVpn, string? networkNote,
        FaceMatchStatus faceMatchStatus, bool isLate, bool hasSelfie, AttendanceApprovalStatus approvalStatus)
    {
        var flags = new List<AttendanceFlag>();

        if (isProxyVpn == true)
            flags.Add(new AttendanceFlag("ProxyVpn", networkNote ?? "VPN / Proxy network", "critical"));

        if (faceMatchStatus == FaceMatchStatus.NotMatched)
            flags.Add(new AttendanceFlag("FaceMismatch", "Selfie doesn't match employee photo", "critical"));

        if (withinFence == false)
            flags.Add(new AttendanceFlag("OutsideFence", "Checked in outside the office area", "warning"));

        if (approvalStatus == AttendanceApprovalStatus.Pending && !hasSelfie)
            flags.Add(new AttendanceFlag("NoSelfie", "No selfie captured for a review-required check-in", "warning"));

        if (isLate)
            flags.Add(new AttendanceFlag("Late", "Late check-in", "info"));

        return flags;
    }
}
