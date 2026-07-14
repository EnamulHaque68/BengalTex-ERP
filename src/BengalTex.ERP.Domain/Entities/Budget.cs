using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Phase A7a — an annual budget for a <see cref="FinancialYear"/>. Holds one
/// <see cref="BudgetLine"/> per account (optionally per cost center) with 12 FY-relative monthly
/// amounts. Planning data only — posts no journal; the Budget-vs-Actual report compares it to
/// posted GL movement.
/// </summary>
public class Budget : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;        // "BUD-####"

    public int FinancialYearId { get; set; }
    public FinancialYear FinancialYear { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

    public string? Notes { get; set; }

    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}

/// <summary>Phase A7a — a budget line: an account (optionally a cost center) with 12 FY-relative monthly amounts.</summary>
public class BudgetLine : BaseTransactionalEntity
{
    public long BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    /// <summary>Optional cost-center dimension (Phase A3) for line/department budgeting.</summary>
    public int? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    // FY-relative months (M1 = the FY's first month).
    public decimal M1 { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal M4 { get; set; }
    public decimal M5 { get; set; }
    public decimal M6 { get; set; }
    public decimal M7 { get; set; }
    public decimal M8 { get; set; }
    public decimal M9 { get; set; }
    public decimal M10 { get; set; }
    public decimal M11 { get; set; }
    public decimal M12 { get; set; }
}

public enum BudgetStatus
{
    Draft = 1,
    Approved = 2
}
