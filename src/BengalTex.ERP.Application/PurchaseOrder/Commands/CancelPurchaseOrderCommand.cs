using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

/// <summary>
/// Cancels a purchase order. Allowed from Draft, Approved or Sent — anything past
/// receiving stays as the historical record. Lifecycle: * → Cancelled.
/// </summary>
public sealed record CancelPurchaseOrderCommand(long Id) : IRequest<ApiResponse<PurchaseOrderDto>>;

internal sealed class CancelPurchaseOrderCommandHandler
    : IRequestHandler<CancelPurchaseOrderCommand, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CancelPurchaseOrderCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        CancelPurchaseOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");

        var status = po.Status;
        if (status != Domain.Entities.PurchaseOrderStatus.Draft &&
            status != Domain.Entities.PurchaseOrderStatus.Approved &&
            status != Domain.Entities.PurchaseOrderStatus.Sent)
        {
            return ApiResponse<PurchaseOrderDto>.Fail(
                "Purchase orders can only be cancelled before receiving begins.");
        }

        po.Status = Domain.Entities.PurchaseOrderStatus.Cancelled;
        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), cancellationToken);
    }
}
