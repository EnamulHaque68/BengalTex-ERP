using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A category of leave (Casual, Sick, Annual, Maternity, Unpaid …). Each type carries
/// an annual entitlement; per-employee per-year running balances live in
/// <see cref="LeaveBalance"/>. <see cref="IsPaid"/>=false means approved days do not
/// reduce payroll absence-deduction (treated as unpaid leave; payroll still deducts).
/// </summary>
public class LeaveType : BaseEntity
{
    public string Code { get; set; } = string.Empty;   // "CL", "SL", "AL", "UL"
    public string Name { get; set; } = string.Empty;

    /// <summary>Paid leave reduces no salary; unpaid leave is treated like Absent in payroll.</summary>
    public bool IsPaid { get; set; } = true;

    /// <summary>Annual entitlement in days (e.g. 10 for Casual). 0 for unpaid types.</summary>
    public decimal AnnualEntitlement { get; set; }

    /// <summary>Optional cap on consecutive days that can be taken in one application.</summary>
    public int? MaxConsecutiveDays { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
