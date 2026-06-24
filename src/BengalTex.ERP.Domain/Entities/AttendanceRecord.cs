using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Daily attendance for an <see cref="Employee"/> — one row per employee per date
/// (unique). v1 is manual/supervisor entry. Payroll consumes Status + OvertimeHours.
/// Transactional (long key) — high volume (employees × days).
/// </summary>
public class AttendanceRecord : BaseTransactionalEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly AttendanceDate { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    /// <summary>"HH:mm" — informational, nullable (Absent/Leave/Holiday have none).</summary>
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }

    public decimal OvertimeHours { get; set; }

    public string? Notes { get; set; }

    // ── Geo-fence verification (populated when self-check-in via GPS) ──
    /// <summary>WGS84 latitude captured at check-in (browser/mobile geolocation).</summary>
    public double? CheckInLatitude { get; set; }
    /// <summary>WGS84 longitude captured at check-in.</summary>
    public double? CheckInLongitude { get; set; }
    /// <summary>Distance in meters from the factory geo-fence centre at check-in time.</summary>
    public double? CheckInDistanceMeters { get; set; }
    /// <summary>
    /// True = inside the configured geo-fence radius. False = outside (flagged).
    /// Null = no GPS provided OR factory has no geo-fence configured (legacy + admin entries).
    /// Per business rule, OUTSIDE-fence check-ins are still accepted but flagged for review.
    /// </summary>
    public bool? CheckInWithinFence { get; set; }

    // ════════════════ Attendance upgrade (P1a foundation — all additive/nullable) ════════════════

    /// <summary>Office / Remote / Field-visit. Office is the only active mode in v1.</summary>
    public AttendanceMode Mode { get; set; } = AttendanceMode.Office;

    /// <summary>The authorized office location whose geo-fence matched at check-in (multi-location).</summary>
    public int? MatchedOfficeLocationId { get; set; }
    public OfficeLocation? MatchedOfficeLocation { get; set; }

    // ── Check-out geo capture (mirrors check-in) ──
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public double? CheckOutDistanceMeters { get; set; }
    public bool? CheckOutWithinFence { get; set; }
    /// <summary>Reverse-geocoded human address of the check-out GPS point (best-effort). Mirrors CheckInAddress.</summary>
    public string? CheckOutAddress { get; set; }

    /// <summary>Total worked minutes (check-out − check-in − breaks). Null until checked out.</summary>
    public int? WorkedMinutes { get; set; }

    // ── Office-time classification (computed against AttendanceSettings on check-in/out) ──
    public bool IsLate { get; set; }
    public bool IsEarlyLeave { get; set; }
    public bool IsOffdayWork { get; set; }
    public bool IsHolidayWork { get; set; }

    // ── Selfie verification (anti buddy-punch) + future AI face-match (reserved) ──
    /// <summary>Storage path of the check-in selfie (via IFileStorage). Supervisor reviews it.</summary>
    public string? CheckInSelfieUrl { get; set; }
    public string? CheckOutSelfieUrl { get; set; }
    /// <summary>Reserved for future AI: similarity score vs the employee's profile photo (0–100).</summary>
    public decimal? FaceMatchScore { get; set; }
    public FaceMatchStatus FaceMatchStatus { get; set; } = FaceMatchStatus.NotChecked;

    // ── Supervisor approval (selfie-verified / flagged rows) ──
    public AttendanceApprovalStatus ApprovalStatus { get; set; } = AttendanceApprovalStatus.NotRequired;
    public int? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedByEmployee { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // ════════════════ Location & network intelligence (P2 — all additive/nullable, best-effort) ════════════════

    /// <summary>Reverse-geocoded human address of the check-in GPS point (e.g. "Tejgaon I/A, Dhaka 1208"). Best-effort.</summary>
    public string? CheckInAddress { get; set; }

    /// <summary>Public IP the check-in request originated from (audit / fraud trail).</summary>
    public string? CheckInIpAddress { get; set; }

    /// <summary>Parsed from the User-Agent: "Mobile" / "Tablet" / "Desktop".</summary>
    public string? CheckInDeviceType { get; set; }
    /// <summary>Parsed browser family (Chrome / Safari / Edge / Firefox / …).</summary>
    public string? CheckInBrowser { get; set; }
    /// <summary>Parsed OS family (Android / iOS / Windows / macOS / Linux / …).</summary>
    public string? CheckInOs { get; set; }

    /// <summary>
    /// True = the check-in IP is flagged as VPN / proxy / TOR / datacenter-hosting (possible
    /// location spoofing). False = clean residential/mobile IP. Null = not checked / unknown / private IP.
    /// Per policy this is a FLAG only — it never blocks the check-in.
    /// </summary>
    public bool? CheckInIsProxyVpn { get; set; }
    /// <summary>ISP / organization owning the check-in IP (e.g. "Grameenphone", "Amazon AWS").</summary>
    public string? CheckInIsp { get; set; }
    /// <summary>Short network note explaining the proxy/VPN flag (e.g. "VPN/Proxy", "TOR exit", "Datacenter", "Private IP").</summary>
    public string? CheckInNetworkNote { get; set; }

    public ICollection<AttendanceBreak> Breaks { get; set; } = new List<AttendanceBreak>();
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    HalfDay = 4,
    Leave = 5,
    Holiday = 6,
    // ── upgrade additions ──
    OnTime = 7,
    EarlyLeave = 8,
    OffdayWork = 9,
    HolidayWork = 10,
    Overtime = 11,
    ManualAdjustment = 12
}

/// <summary>
/// Shared classification of attendance statuses so payroll, dashboards and the "My Attendance"
/// view never drift apart. The upgrade added several "present-like" statuses (OnTime, EarlyLeave,
/// OffdayWork, HolidayWork, Overtime) — all of these still mean the employee was physically at work
/// and must be counted as a present day exactly like the legacy <see cref="AttendanceStatus.Present"/>.
/// </summary>
public static class AttendanceStatusExtensions
{
    /// <summary>True when the status represents a full present working day (excludes HalfDay, which is counted as 0.5).</summary>
    public static bool CountsAsFullPresent(this AttendanceStatus status) => status
        is AttendanceStatus.Present
        or AttendanceStatus.Late
        or AttendanceStatus.OnTime
        or AttendanceStatus.EarlyLeave
        or AttendanceStatus.OffdayWork
        or AttendanceStatus.HolidayWork
        or AttendanceStatus.Overtime;

    /// <summary>True when the employee was present in any capacity (full day or half day).</summary>
    public static bool CountsAsPresent(this AttendanceStatus status)
        => status.CountsAsFullPresent() || status == AttendanceStatus.HalfDay;
}

/// <summary>Where the attendance was given. Office is the only active mode in v1; rest are future-ready.</summary>
public enum AttendanceMode
{
    Office = 1,
    Remote = 2,
    FieldVisit = 3
}

/// <summary>Supervisor approval state for a check-in (used when selfie/flag review is required).</summary>
public enum AttendanceApprovalStatus
{
    NotRequired = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>Reserved for future AI face-match between the check-in selfie and the employee photo.</summary>
public enum FaceMatchStatus
{
    NotChecked = 0,
    Matched = 1,
    NotMatched = 2,
    Inconclusive = 3
}
