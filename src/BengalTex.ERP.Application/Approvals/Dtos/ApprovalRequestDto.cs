namespace BengalTex.ERP.Application.Approvals.Dtos;

public sealed record ApprovalRequestDto(
    long Id,
    string DocumentType,
    long DocumentId,
    string DocumentReference,
    string Status,                       // Pending | Approved | Rejected | Cancelled
    int CurrentLevel,
    int TotalLevels,
    string? CurrentApproverRole,         // role expected to act at the current level (null when done)
    string? RequestedBy,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ApprovalStepDto> Steps);

public sealed record ApprovalStepDto(
    long Id,
    int Level,
    string ApproverRole,
    string? ApproverUserId,
    string Status,                       // Pending | Approved | Rejected | Skipped
    DateTimeOffset? ActedAt,
    string? Comment);
