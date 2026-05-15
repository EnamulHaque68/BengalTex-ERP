using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

/// <summary>Approves a draft PO. Lifecycle: Draft → Approved.</summary>
public sealed record ApprovePurchaseOrderCommand(long Id) : IRequest<ApiResponse<PurchaseOrderDto>>;

internal sealed class ApprovePurchaseOrderCommandHandler
    : IRequestHandler<ApprovePurchaseOrderCommand, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ApprovePurchaseOrderCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        ApprovePurchaseOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");
        if (po.Status != Domain.Entities.PurchaseOrderStatus.Draft)
            return ApiResponse<PurchaseOrderDto>.Fail("Only draft purchase orders can be approved.");

        po.Status = Domain.Entities.PurchaseOrderStatus.Approved;
        po.ApprovedAt = DateTimeOffset.UtcNow;
        po.ApprovedBy = _currentUser.UserName;
        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), cancellationToken);
    }
}
