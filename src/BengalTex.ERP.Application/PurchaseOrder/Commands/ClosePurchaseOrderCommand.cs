using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

/// <summary>
/// Marks a (partially- or fully-) received PO as closed. Closure separates physical
/// receiving (auto-set by GRN postings) from final settlement — a manual decision
/// that also accommodates short-ship scenarios (close on partial when both parties
/// agree the order is settled). Lifecycle: PartiallyReceived | Received → Closed.
/// </summary>
public sealed record ClosePurchaseOrderCommand(long Id) : IRequest<ApiResponse<PurchaseOrderDto>>;

internal sealed class ClosePurchaseOrderCommandHandler
    : IRequestHandler<ClosePurchaseOrderCommand, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ClosePurchaseOrderCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        ClosePurchaseOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");
        if (po.Status != Domain.Entities.PurchaseOrderStatus.Received &&
            po.Status != Domain.Entities.PurchaseOrderStatus.PartiallyReceived)
        {
            return ApiResponse<PurchaseOrderDto>.Fail(
                "A purchase order can only be closed once receiving has started.");
        }

        po.Status = Domain.Entities.PurchaseOrderStatus.Closed;
        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), cancellationToken);
    }
}
