namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Approval-workflow rules (bound from the "Approvals" config section). v1 is a single
/// threshold + approver role per gated document type — Purchase Orders only for now.
/// </summary>
public class ApprovalSettings
{
    /// <summary>PO total at or below this amount is auto-approved; above it requires sign-off.</summary>
    public decimal PurchaseOrderThreshold { get; set; } = 50000m;

    /// <summary>Role whose members can approve over-threshold purchase orders.</summary>
    public string PurchaseOrderApproverRole { get; set; } = "Admin";
}
