using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// An employee's request for time off. Lifecycle Pending → Approved | Rejected | Cancelled.
///
/// <para><b>Day computation</b>: TotalDays = inclusive days in [FromDate, ToDate] minus
/// weekend days (Bangladesh: Friday) and minus any active <see cref="Holiday"/> dates in the range.</para>
///
/// <para><b>Approve</b> atomically increments <see cref="LeaveBalance.Taken"/> AND
/// (if <see cref="WriteAttendance"/>=true) writes AttendanceRecord rows with Status=Leave
/// for each working day in the range. Cancel of an Approved app reverses both.</para>
/// </summary>
public class LeaveApplication : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;   // "LV-####"

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }

    /// <summary>Working days in the range (computed by service; persisted as snapshot).</summary>
    public decimal TotalDays { get; set; }

    public string? Reason { get; set; }

    public LeaveApplicationStatus Status { get; set; } = LeaveApplicationStatus.Pending;

    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>If true, Approve writes AttendanceRecord Status=Leave for each working day in range.</summary>
    public bool WriteAttendance { get; set; } = true;

    public string? Notes { get; set; }
}

public enum LeaveApplicationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
