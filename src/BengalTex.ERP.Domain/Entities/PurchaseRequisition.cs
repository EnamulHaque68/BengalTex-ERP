using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Purchase Requisition — an internal request for raw materials raised by a department BEFORE
/// a <see cref="PurchaseOrder"/> is created. Adds an approval gate to procurement: anyone
/// authorised can Submit a draft; an approver decides Approve|Reject; approved requisitions
/// can then be Converted into a real PO (caller picks the supplier and final price — the PR
/// only carries the indicative estimate).
///
/// Lifecycle: Draft → Submitted → Approved | Rejected | Cancelled; Approved → Converted (one-way).
/// </summary>
public class PurchaseRequisition : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;          // "PR-####"

    public DateOnly RequisitionDate { get; set; }
    public DateOnly? NeededByDate { get; set; }

    /// <summary>Optional department FK (HR master) — captures who's asking. Free-text dept allowed via Department field.</summary>
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    /// <summary>Free-text department/cost-centre, used when no FK is appropriate.</summary>
    public string? DepartmentText { get; set; }

    /// <summary>Free-text "raised by" — name or employee code (we don't FK to Identity here for simplicity).</summary>
    public string? RequestedBy { get; set; }

    /// <summary>Why this material is needed (production order, urgency context, etc.).</summary>
    public string? Purpose { get; set; }

    public PurchaseRequisitionStatus Status { get; set; } = PurchaseRequisitionStatus.Draft;

    public DateTimeOffset? SubmittedAt { get; set; }
    public string? SubmittedByUser { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedByUser { get; set; }
    public string? DecisionNotes { get; set; }

    public DateTimeOffset? ConvertedAt { get; set; }
    public long? ConvertedPurchaseOrderId { get; set; }
    public PurchaseOrder? ConvertedPurchaseOrder { get; set; }

    public string? Notes { get; set; }

    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = new List<PurchaseRequisitionLine>();
}

public class PurchaseRequisitionLine : BaseTransactionalEntity
{
    public long PurchaseRequisitionId { get; set; }
    public PurchaseRequisition PurchaseRequisition { get; set; } = null!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public decimal Quantity { get; set; }

    /// <summary>Indicative price for budgeting only; the eventual PO records the actual negotiated price.</summary>
    public decimal EstimatedUnitPrice { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum PurchaseRequisitionStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5,
    Converted = 6      // a real PurchaseOrder was raised from this PR
}
