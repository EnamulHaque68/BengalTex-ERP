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

    public DeleteProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
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

        _repo.Remove(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Production order deleted.");
    }
}
