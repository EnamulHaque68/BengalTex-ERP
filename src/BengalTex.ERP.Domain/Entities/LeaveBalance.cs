using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Per-employee per-leave-type running balance for a calendar year.
/// Initialised at year start (or on-demand) from <see cref="LeaveType.AnnualEntitlement"/>;
/// Taken increments on leave Approve; Remaining = Entitled − Taken.
/// One row per (EmployeeId, LeaveTypeId, Year) — unique.
/// </summary>
public class LeaveBalance : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int Year { get; set; }

    public decimal Entitled { get; set; }
    public decimal Taken { get; set; }
    public decimal Remaining => Entitled - Taken;
}
