using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Approvals.Commands;

/// <summary>
/// Approve or reject the current pending step of an approval request. On completion the
/// gated document is advanced: a PurchaseOrder moves to Approved (on full approval) or
/// back to Draft (on rejection). The approval-step change and the document-status change
/// commit in one SaveChanges (atomic).
/// </summary>
public sealed record DecideApprovalRequestCommand(long Id, bool Approve, string? Comment)
    : IRequest<ApiResponse<bool>>;

internal sealed class DecideApprovalRequestCommandHandler
    : IRequestHandler<DecideApprovalRequestCommand, ApiResponse<bool>>
{
    private readonly IApprovalService _approval;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;

    public DecideApprovalRequestCommandHandler(
        IApprovalService approval,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo)
    {
        _approval = approval;
        _currentUser = currentUser;
        _uow = uow;
        _poRepo = poRepo;
    }

    public async Task<ApiResponse<bool>> Handle(
        DecideApprovalRequestCommand cmd, CancellationToken cancellationToken)
    {
        var roles = _currentUser.Roles;
        var isSuperAdmin = roles.Contains("SuperAdmin");

        var result = await _approval.DecideAsync(
            cmd.Id, cmd.Approve, _currentUser.UserName ?? _currentUser.UserId,
            roles, isSuperAdmin, cmd.Comment, cancellationToken);

        if (!result.Success)
            return ApiResponse<bool>.Fail(result.Error ?? "Could not process the decision.");

        // Advance the gated document (v1: PurchaseOrder only).
        if (result.DocumentType == "PurchaseOrder" &&
            result.Outcome != ApprovalOutcome.StillPending)
        {
            var po = await _poRepo.GetByIdAsync(result.DocumentId, cancellationToken);
            if (po is not null && po.Status == PurchaseOrderStatus.PendingApproval)
            {
                if (result.Outcome == ApprovalOutcome.Approved)
                {
                    po.Status = PurchaseOrderStatus.Approved;
                    po.ApprovedAt = DateTimeOffset.UtcNow;
                    po.ApprovedBy = _currentUser.UserName;
                }
                else // Rejected → return to Draft for editing / resubmission
                {
                    po.Status = PurchaseOrderStatus.Draft;
                }
                _poRepo.Update(po);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var message = cmd.Approve
            ? (result.Outcome == ApprovalOutcome.Approved ? "Approved." : "Approved this level.")
            : "Rejected.";
        return ApiResponse<bool>.Ok(true, message);
    }
}
