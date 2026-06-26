using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

public sealed record DeleteProductionOrderCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteProductionOrderCommandHandler
    : IRequestHandler<DeleteProductionOrderCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStockReservationService _reservations;

    public DeleteProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IUnitOfWork uow,
        IStockReservationService reservations)
    {
        _repo = repo;
        _uow = uow;
        _reservations = reservations;
    }

    public async Task<ApiResponse> Handle(DeleteProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse.Fail("Production order not found.");

        // Once started or completed, production is part of the audit trail — only Draft / Cancelled may be deleted
        if (po.Status != Domain.Entities.ProductionOrderStatus.Draft &&
            po.Status != Domain.Entities.ProductionOrderStatus.Cancelled)
        {
            return ApiResponse.Fail("Only draft or cancelled production orders can be deleted. Cancel it first.");
        }

        // Phase 2 — release any active reservations (a draft PO still holds them; idempotent otherwise).
        await _reservations.ReleaseForReferenceAsync("ProductionOrder", po.Id, cancellationToken);

        _repo.Remove(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Production order deleted.");
    }
}
