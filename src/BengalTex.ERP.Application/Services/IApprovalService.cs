using BengalTex.ERP.Application.Approvals.Dtos;

namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Generic approval-workflow engine. Lives behind this interface (impl in Infrastructure)
/// because <c>ApprovalRequest</c>/<c>ApprovalStep</c> are Infrastructure cross-cutting
/// entities — same inversion pattern as <see cref="IStockService"/>.
///
/// Mutating methods (Submit/Decide) only change the tracked context; the CALLER owns
/// SaveChanges so the document-status update and the approval rows commit atomically.
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Raises an approval request for a document. Auto-approves (no pending step) when
    /// <paramref name="amount"/> is within the configured threshold; otherwise creates a
    /// single pending step for the configured approver role.
    /// </summary>
    Task<ApprovalSubmitResult> SubmitAsync(
        string documentType, long documentId, string documentReference,
        decimal amount, CancellationToken ct = default);

    /// <summary>Pending requests the given roles may act on (SuperAdmin sees all pending).</summary>
    Task<IReadOnlyList<ApprovalRequestDto>> GetInboxAsync(
        IReadOnlyList<string> roles, bool seeAll, CancellationToken ct = default);

    /// <summary>All requests, newest first, optionally filtered by status.</summary>
    Task<IReadOnlyList<ApprovalRequestDto>> GetAllAsync(string? status, CancellationToken ct = default);

    Task<ApprovalRequestDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Approve or reject the current pending step of a request.</summary>
    Task<ApprovalActResult> DecideAsync(
        long requestId, bool approve, string? userId,
        IReadOnlyList<string> roles, bool isSuperAdmin, string? comment,
        CancellationToken ct = default);
}

public sealed record ApprovalSubmitResult(bool AutoApproved);

public enum ApprovalOutcome { StillPending, Approved, Rejected }

public sealed record ApprovalActResult(
    bool Success,
    string? Error,
    ApprovalOutcome Outcome,
    string DocumentType,
    long DocumentId);
