using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Inventory.Commands;

public sealed record DeleteStockAdjustmentCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteStockAdjustmentCommandHandler
    : IRequestHandler<DeleteStockAdjustmentCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.StockAdjustment, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteStockAdjustmentCommandHandler(
        IRepository<Domain.Entities.StockAdjustment, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteStockAdjustmentCommand cmd, CancellationToken cancellationToken)
    {
        var adj = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (adj is null) return ApiResponse.Fail("Stock adjustment not found.");

        // Posted adjustments have already created stock movements — immutable
        if (adj.Status != Domain.Entities.StockAdjustmentStatus.Draft)
            return ApiResponse.Fail("Only draft stock adjustments can be deleted.");

        _repo.Remove(adj);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Stock adjustment deleted.");
    }
}
