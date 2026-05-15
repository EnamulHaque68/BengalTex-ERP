using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.GoodsReceipt.Commands;

public sealed record DeleteGoodsReceiptCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteGoodsReceiptCommandHandler
    : IRequestHandler<DeleteGoodsReceiptCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteGoodsReceiptCommandHandler(
        IRepository<Domain.Entities.GoodsReceiptNote, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteGoodsReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var grn = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (grn is null) return ApiResponse.Fail("Goods receipt not found.");

        // Posted GRNs are immutable — they've already updated PO line ReceivedQuantity
        if (grn.Status != Domain.Entities.GoodsReceiptStatus.Draft)
            return ApiResponse.Fail("Only draft goods receipts can be deleted.");

        _repo.Remove(grn);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Goods receipt deleted.");
    }
}
