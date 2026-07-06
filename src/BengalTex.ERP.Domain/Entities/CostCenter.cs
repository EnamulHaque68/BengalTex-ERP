using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Cost / profit center (Phase A3) — the primary accounting dimension. A posting can be tagged
/// with a cost center so expense, payroll, production and factory results become sliceable
/// (Weaving vs Printing vs Admin, Factory 1 vs 2). A center can optionally represent an existing
/// <see cref="Department"/> or <see cref="Factory"/>, and supports a parent for roll-up.
/// </summary>
public class CostCenter : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public CostCenterKind Kind { get; set; } = CostCenterKind.Cost;

    public int? ParentCostCenterId { get; set; }
    public CostCenter? ParentCostCenter { get; set; }
    public ICollection<CostCenter> Children { get; set; } = new List<CostCenter>();

    /// <summary>Optional link to the org unit this center represents (drives payroll/expense mapping).</summary>
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? FactoryId { get; set; }
    public Factory? Factory { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public enum CostCenterKind
{
    Cost = 1,     // absorbs cost only (departments, admin)
    Profit = 2,   // earns revenue (product lines, sales channels)
    Both = 3
}
