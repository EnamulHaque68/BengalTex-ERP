using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// An employee-raised request to add or correct a day's attendance (forgot to check in/out,
/// wrong time, regularize an off-day worked, etc.). A supervisor reviews it; on approval the
/// requested values are applied to the <see cref="AttendanceRecord"/> for that employee + date.
/// Transactional (long key) — volume scales with employees × days.
/// </summary>
public class AttendanceRequest : BaseTransactionalEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>The attendance date this request adds / corrects.</summary>
    public DateOnly RequestDate { get; set; }

    public AttendanceRequestType RequestType { get; set; } = AttendanceRequestType.TimeCorrection;

    /// <summary>"HH:mm" requested check-in (null = leave as-is / not applicable).</summary>
    public string? RequestedCheckInTime { get; set; }
    /// <summary>"HH:mm" requested check-out.</summary>
    public string? RequestedCheckOutTime { get; set; }
    /// <summary>Optional requested status override (e.g. regularize as Present / OffdayWork).</summary>
    public AttendanceStatus? RequestedStatus { get; set; }

    /// <summary>Why the correction is needed (required).</summary>
    public string Reason { get; set; } = string.Empty;

    public AttendanceRequestStatus Status { get; set; } = AttendanceRequestStatus.Pending;

    // ── Supervisor review ──
    public int? ReviewedByEmployeeId { get; set; }
    public Employee? ReviewedByEmployee { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    /// <summary>The attendance row created / updated when this request was approved.</summary>
    public long? AppliedAttendanceRecordId { get; set; }
}

public enum AttendanceRequestType
{
    MissingCheckIn = 1,
    MissingCheckOut = 2,
    TimeCorrection = 3,
    Regularization = 4,
    OffdayWork = 5,
    Other = 6
}

public enum AttendanceRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
