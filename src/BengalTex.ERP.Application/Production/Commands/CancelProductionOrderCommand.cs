using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>
/// Cancels a production order. Allowed from Draft or InProgress only — once Completed,
/// the run is part of the stock-movement audit trail and cannot be cancelled. Cancellation
/// posts no stock movements (movements only happen at Complete).
/// </summary>
public sealed record CancelProductionOrderCommand(long Id) : IRequest<ApiResponse<ProductionOrderDto>>;

internal sealed class CancelProductionOrderCommandHandler
    : IRequestHandler<CancelProductionOrderCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStockReservationService _reservations;
    private readonly IMediator _mediator;

    public CancelProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IUnitOfWork uow,
        IStockReservationService reservations,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _reservations = reservations;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        CancelProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");

        if (po.Status != Domain.Entities.ProductionOrderStatus.Draft &&
            po.Status != Domain.Entities.ProductionOrderStatus.InProgress)
        {
            return ApiResponse<ProductionOrderDto>.Fail(
                "Only draft or in-progress production orders can be cancelled.");
        }

        po.Status = Domain.Entities.ProductionOrderStatus.Cancelled;
        _repo.Update(po);

        // Phase 2 — free any reserved raw materials back to available stock.
        await _reservations.ReleaseForReferenceAsync("ProductionOrder", po.Id, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
