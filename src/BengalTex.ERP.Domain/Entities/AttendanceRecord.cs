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
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    HalfDay = 4,
    Leave = 5,
    Holiday = 6
}
