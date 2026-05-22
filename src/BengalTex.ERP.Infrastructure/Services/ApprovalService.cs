using BengalTex.ERP.Application.Approvals.Dtos;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IApprovalService"/> over the ApprovalRequests/ApprovalSteps tables.
/// v1: single-level, threshold-gated, role-based approval. Mutating methods do NOT save —
/// the calling command handler owns SaveChanges (atomic with the document-status change).
/// </summary>
public sealed class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly ApprovalSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ApprovalService(
        ApplicationDbContext db,
        IOptions<ApprovalSettings> settings,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        INotificationService notifications)
    {
        _db = db;
        _settings = settings.Value;
        _clock = clock;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<ApprovalSubmitResult> SubmitAsync(
        string documentType, long documentId, string documentReference,
        decimal amount, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var auto = amount <= _settings.PurchaseOrderThreshold;

        var request = new ApprovalRequest
        {
            DocumentType = documentType,
            DocumentId = documentId,
            DocumentReference = documentReference,
            RequestedBy = _currentUser.UserName ?? _currentUser.UserId,
            RequestedAt = now,
            CurrentLevel = 1,
            TotalLevels = 1,
            Status = auto ? ApprovalStatus.Approved : ApprovalStatus.Pending,
            CompletedAt = auto ? now : null,
        };

        request.Steps.Add(new ApprovalStep
        {
            Level = 1,
            ApproverRole = _settings.PurchaseOrderApproverRole,
            Status = auto ? ApprovalStepStatus.Skipped : ApprovalStepStatus.Pending,
            ApproverUserId = auto ? "system" : null,
            ActedAt = auto ? now : null,
            Comment = auto ? $"Auto-approved (within {_settings.PurchaseOrderThreshold:N0} threshold)" : null,
        });

        _db.ApprovalRequests.Add(request);   // caller saves

        // Notify the approver role that a request is waiting (record-only; caller commits).
        if (!auto)
        {
            await _notifications.NotifyAsync(
                NotificationChannels.InApp,
                _settings.PurchaseOrderApproverRole,
                $"{documentType} {documentReference} pending approval",
                $"{documentType} {documentReference} (amount {amount:N2}) was submitted by " +
                $"{_currentUser.UserName ?? _currentUser.UserId} and is awaiting your approval.",
                documentType, documentId, ct);
        }

        return new ApprovalSubmitResult(auto);
    }

    public async Task<IReadOnlyList<ApprovalRequestDto>> GetInboxAsync(
        IReadOnlyList<string> roles, bool seeAll, CancellationToken ct = default)
    {
        var pending = await _db.ApprovalRequests.AsNoTracking()
            .Include(r => r.Steps)
            .Where(r => r.Status == ApprovalStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        IEnumerable<ApprovalRequest> result = pending;
        if (!seeAll)
        {
            result = pending.Where(r =>
            {
                var step = r.Steps.FirstOrDefault(s => s.Level == r.CurrentLevel);
                return step is not null && roles.Contains(step.ApproverRole);
            });
        }

        return result.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ApprovalRequestDto>> GetAllAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.ApprovalRequests.AsNoTracking().Include(r => r.Steps).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ApprovalStatus>(status, true, out var parsed))
            query = query.Where(r => r.Status == parsed);

        var list = await query.OrderByDescending(r => r.RequestedAt).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<ApprovalRequestDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var request = await _db.ApprovalRequests.AsNoTracking()
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        return request is null ? null : Map(request);
    }

    public async Task<ApprovalActResult> DecideAsync(
        long requestId, bool approve, string? userId,
        IReadOnlyList<string> roles, bool isSuperAdmin, string? comment,
        CancellationToken ct = default)
    {
        var request = await _db.ApprovalRequests
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (request is null)
            return Fail("Approval request not found.");
        if (request.Status != ApprovalStatus.Pending)
            return Fail($"This request is already {request.Status}.");

        var step = request.Steps.FirstOrDefault(s => s.Level == request.CurrentLevel && s.Status == ApprovalStepStatus.Pending);
        if (step is null)
            return Fail("No pending step to act on.");
        if (!isSuperAdmin && !roles.Contains(step.ApproverRole))
            return Fail($"You are not authorized to approve at this level (requires role '{step.ApproverRole}').");

        var now = _clock.UtcNow;
        step.ApproverUserId = userId;
        step.ActedAt = now;
        step.Comment = comment;

        ApprovalOutcome outcome;
        if (approve)
        {
            step.Status = ApprovalStepStatus.Approved;
            if (request.CurrentLevel >= request.TotalLevels)
            {
                request.Status = ApprovalStatus.Approved;
                request.CompletedAt = now;
                outcome = ApprovalOutcome.Approved;
            }
            else
            {
                request.CurrentLevel++;          // advance to next level (multi-level ready)
                outcome = ApprovalOutcome.StillPending;
            }
        }
        else
        {
            step.Status = ApprovalStepStatus.Rejected;
            request.Status = ApprovalStatus.Rejected;
            request.CompletedAt = now;
            outcome = ApprovalOutcome.Rejected;
        }

        // caller saves
        return new ApprovalActResult(true, null, outcome, request.DocumentType, request.DocumentId);

        static ApprovalActResult Fail(string error) =>
            new(false, error, ApprovalOutcome.StillPending, string.Empty, 0);
    }

    private static ApprovalRequestDto Map(ApprovalRequest r)
    {
        var currentRole = r.Status == ApprovalStatus.Pending
            ? r.Steps.FirstOrDefault(s => s.Level == r.CurrentLevel)?.ApproverRole
            : null;

        return new ApprovalRequestDto(
            r.Id, r.DocumentType, r.DocumentId, r.DocumentReference,
            r.Status.ToString(), r.CurrentLevel, r.TotalLevels, currentRole,
            r.RequestedBy, r.RequestedAt, r.CompletedAt,
            r.Steps.OrderBy(s => s.Level).Select(s => new ApprovalStepDto(
                s.Id, s.Level, s.ApproverRole, s.ApproverUserId,
                s.Status.ToString(), s.ActedAt, s.Comment)).ToList());
    }
}
