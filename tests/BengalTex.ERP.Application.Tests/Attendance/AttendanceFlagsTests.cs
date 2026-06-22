using System.Linq;
using BengalTex.ERP.Application.Attendance;
using BengalTex.ERP.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Application.Tests.Attendance;

public class AttendanceFlagsTests
{
    [Fact]
    public void CleanCheckIn_HasNoFlags()
    {
        var flags = AttendanceFlags.For(
            withinFence: true, isProxyVpn: false, networkNote: null,
            faceMatchStatus: FaceMatchStatus.NotChecked, isLate: false, hasSelfie: true,
            approvalStatus: AttendanceApprovalStatus.NotRequired);
        flags.Should().BeEmpty();
    }

    [Fact]
    public void Vpn_IsCriticalFlag()
    {
        var flags = AttendanceFlags.For(true, true, "VPN / Proxy", FaceMatchStatus.NotChecked, false, true, AttendanceApprovalStatus.Pending);
        flags.Should().ContainSingle(f => f.Code == "ProxyVpn" && f.Severity == "critical");
    }

    [Fact]
    public void FaceMismatch_IsCriticalFlag()
    {
        var flags = AttendanceFlags.For(true, false, null, FaceMatchStatus.NotMatched, false, true, AttendanceApprovalStatus.Pending);
        flags.Should().Contain(f => f.Code == "FaceMismatch" && f.Severity == "critical");
    }

    [Fact]
    public void OutsideFence_IsWarning()
    {
        var flags = AttendanceFlags.For(false, false, null, FaceMatchStatus.NotChecked, false, true, AttendanceApprovalStatus.Pending);
        flags.Should().Contain(f => f.Code == "OutsideFence" && f.Severity == "warning");
    }

    [Fact]
    public void NoSelfie_OnlyFlaggedWhenReviewPending()
    {
        AttendanceFlags.For(true, false, null, FaceMatchStatus.NotChecked, false, hasSelfie: false,
            approvalStatus: AttendanceApprovalStatus.Pending)
            .Should().Contain(f => f.Code == "NoSelfie");

        // Not pending review → missing selfie is not flagged
        AttendanceFlags.For(true, false, null, FaceMatchStatus.NotChecked, false, hasSelfie: false,
            approvalStatus: AttendanceApprovalStatus.NotRequired)
            .Should().NotContain(f => f.Code == "NoSelfie");
    }

    [Fact]
    public void Late_IsInfoSeverity()
    {
        var flags = AttendanceFlags.For(true, false, null, FaceMatchStatus.NotChecked, isLate: true, true, AttendanceApprovalStatus.NotRequired);
        flags.Should().ContainSingle(f => f.Code == "Late" && f.Severity == "info");
    }

    [Fact]
    public void MultipleSignals_AllSurface_CriticalFirst()
    {
        var flags = AttendanceFlags.For(false, true, "TOR exit", FaceMatchStatus.NotMatched, true, false, AttendanceApprovalStatus.Pending);
        flags.Select(f => f.Code).Should().Contain(new[] { "ProxyVpn", "FaceMismatch", "OutsideFence", "NoSelfie", "Late" });
        flags.First().Severity.Should().Be("critical");
    }
}
