using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>
/// Releases the QC hold on a completed production run (Phase 5). The finished goods were soft-reserved
/// in the receive warehouse at Complete (when <c>RequiresQc</c>); this clears that reservation so the
/// goods become usable/dispatchable, and stamps <c>QcReleasedAt</c>. Use after QC has passed.
/// </summary>
public sealed record ReleaseProductionQcHoldCommand(long Id) : IRequest<ApiResponse<ProductionOrderDto>>;

internal sealed class ReleaseProductionQcHoldCommandHandler
    : IRequestHandler<ReleaseProductionQcHoldCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IStockReservationService _reservations;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ReleaseProductionQcHoldCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IStockReservationService reservations,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _reservations = reservations;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        ReleaseProductionQcHoldCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");
        if (!po.RequiresQc)
            return ApiResponse<ProductionOrderDto>.Fail("This production order is not under QC hold.");
        if (po.Status != Domain.Entities.ProductionOrderStatus.Completed)
            return ApiResponse<ProductionOrderDto>.Fail("Only completed production orders can be QC-released.");
        if (po.QcReleasedAt.HasValue)
            return ApiResponse<ProductionOrderDto>.Fail("This production order's QC hold has already been released.");

        await _reservations.ReleaseForReferenceAsync("QcHold", po.Id, cancellationToken);
        po.QcReleasedAt = DateTimeOffset.UtcNow;
        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
