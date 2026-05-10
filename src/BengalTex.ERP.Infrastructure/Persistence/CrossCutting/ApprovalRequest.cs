using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Persistence.CrossCutting;

public class ApprovalRequest : BaseTransactionalEntity
{
    public string DocumentType { get; set; } = string.Empty; // "SalesOrder", "Bom", "Quotation"
    public long DocumentId { get; set; }
    public string DocumentReference { get; set; } = string.Empty; // Human-readable e.g., SO number
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public int CurrentLevel { get; set; }
    public int TotalLevels { get; set; }
    public string? RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}

public class ApprovalStep : BaseTransactionalEntity
{
    public long ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }
    public int Level { get; set; }
    public string ApproverRole { get; set; } = string.Empty;  // Role-based approver
    public string? ApproverUserId { get; set; }               // Who actually approved
    public ApprovalStepStatus Status { get; set; } = ApprovalStepStatus.Pending;
    public DateTimeOffset? ActedAt { get; set; }
    public string? Comment { get; set; }
}

public enum ApprovalStatus { Pending, Approved, Rejected, Cancelled }
public enum ApprovalStepStatus { Pending, Approved, Rejected, Skipped }