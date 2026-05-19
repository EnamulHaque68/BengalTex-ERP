using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.StockTransfer.Commands;

/// <summary>
/// Deletes a Draft stock transfer (soft delete). Posted transfers are immutable —
/// to reverse, post a counter-transfer.
/// </summary>
public sealed record DeleteStockTransferCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteStockTransferCommandHandler
    : IRequestHandler<DeleteStockTransferCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.StockTransfer, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteStockTransferCommandHandler(
        IRepository<Domain.Entities.StockTransfer, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteStockTransferCommand cmd, CancellationToken cancellationToken)
    {
        var transfer = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (transfer is null) return ApiResponse.Fail("Stock transfer not found.");

        if (transfer.Status != Domain.Entities.StockTransferStatus.Draft)
            return ApiResponse.Fail("Only draft stock transfers can be deleted. Posted transfers are immutable — post a counter-transfer to reverse.");

        _repo.Remove(transfer);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Stock transfer deleted.");
    }
}
