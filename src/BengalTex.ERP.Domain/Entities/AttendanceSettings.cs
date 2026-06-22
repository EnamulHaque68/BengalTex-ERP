using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Company-level attendance policy — office hours, grace, geo-fence behaviour, selfie + approval
/// rules, enabled modes. One row per company (created/edited via the Attendance Settings screen).
/// </summary>
public class AttendanceSettings : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // ── Office hours ──
    public TimeOnly OfficeStartTime { get; set; } = new(9, 0);
    public TimeOnly OfficeEndTime { get; set; } = new(18, 0);
    /// <summary>Minutes after start still counted On-Time (e.g. 15).</summary>
    public int GracePeriodMinutes { get; set; } = 15;

    // ── Geo-fence behaviour ──
    /// <summary>What happens when a check-in is OUTSIDE every authorized fence.</summary>
    public OutsideFenceMode OutsideFenceMode { get; set; } = OutsideFenceMode.Flag;
    /// <summary>Default radius (metres) applied to new office locations.</summary>
    public int DefaultRadiusMeters { get; set; } = 10;

    // ── Selfie + approval (anti buddy-punch) ──
    /// <summary>When true, a live selfie is required at check-in and review goes to the supervisor.</summary>
    public bool RequireSelfie { get; set; }
    /// <summary>When true, self check-ins start as Pending and need supervisor approval.</summary>
    public bool RequireSupervisorApproval { get; set; }

    // ── Enabled attendance modes (Office always on) ──
    public bool AllowRemote { get; set; }
    public bool AllowFieldVisit { get; set; }
}

/// <summary>Policy when a check-in is outside every authorized geo-fence.</summary>
public enum OutsideFenceMode
{
    /// <summary>Accept the check-in but raise a red flag for supervisor review.</summary>
    Flag = 1,
    /// <summary>Block the check-in outright.</summary>
    Block = 2
}
