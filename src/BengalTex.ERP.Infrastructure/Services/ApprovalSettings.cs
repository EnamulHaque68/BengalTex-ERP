namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Approval-workflow rules (bound from the "Approvals" config section). A single threshold
/// + approver role per gated document type. Gated types: Purchase Order, Expense.
/// </summary>
public class ApprovalSettings
{
    /// <summary>PO total at or below this amount is auto-approved; above it requires sign-off.</summary>
    public decimal PurchaseOrderThreshold { get; set; } = 50000m;

    /// <summary>Role whose members can approve over-threshold purchase orders.</summary>
    public string PurchaseOrderApproverRole { get; set; } = "Admin";

    /// <summary>Expense amount at or below this is auto-approved; above it requires sign-off.</summary>
    public decimal ExpenseThreshold { get; set; } = 20000m;

    /// <summary>Role whose members can approve over-threshold expenses.</summary>
    public string ExpenseApproverRole { get; set; } = "Admin";

    /// <summary>SO value at or below this is auto-confirmed; above it requires sign-off.</summary>
    public decimal SalesOrderThreshold { get; set; } = 100000m;

    /// <summary>Role whose members can approve over-threshold sales orders.</summary>
    public string SalesOrderApproverRole { get; set; } = "Admin";

    /// <summary>Manual journal voucher total at or below this posts directly; above it requires sign-off (Phase A1).</summary>
    public decimal JournalEntryThreshold { get; set; } = 100000m;

    /// <summary>Role whose members can approve over-threshold manual journal vouchers.</summary>
    public string JournalEntryApproverRole { get; set; } = "Admin";
}
