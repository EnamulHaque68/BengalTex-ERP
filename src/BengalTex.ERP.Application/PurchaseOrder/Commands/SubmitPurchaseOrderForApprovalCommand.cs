using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

/// <summary>
/// Submits a draft PO into the approval workflow. Within the configured threshold it is
/// auto-approved (Draft → Approved); above it, a pending approval request is raised
/// (Draft → PendingApproval) for an approver to sign off via the Approvals inbox.
/// </summary>
public sealed record SubmitPurchaseOrderForApprovalCommand(long Id) : IRequest<ApiResponse<PurchaseOrderDto>>;

internal sealed class SubmitPurchaseOrderForApprovalCommandHandler
    : IRequestHandler<SubmitPurchaseOrderForApprovalCommand, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IApprovalService _approval;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SubmitPurchaseOrderForApprovalCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IApprovalService approval,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _approval = approval;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        SubmitPurchaseOrderForApprovalCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, cancellationToken);

        if (po is null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");
        if (po.Status != Domain.Entities.PurchaseOrderStatus.Draft)
            return ApiResponse<PurchaseOrderDto>.Fail("Only draft purchase orders can be submitted for approval.");

        var amount = po.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var result = await _approval.SubmitAsync("PurchaseOrder", po.Id, po.Code, amount, cancellationToken);

        if (result.AutoApproved)
        {
            po.Status = Domain.Entities.PurchaseOrderStatus.Approved;
            po.ApprovedAt = DateTimeOffset.UtcNow;
            po.ApprovedBy = _currentUser.UserName;
        }
        else
        {
            po.Status = Domain.Entities.PurchaseOrderStatus.PendingApproval;
        }

        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), cancellationToken);
    }
}
