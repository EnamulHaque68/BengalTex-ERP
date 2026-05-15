using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

public sealed record DeletePurchaseOrderCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeletePurchaseOrderCommandHandler
    : IRequestHandler<DeletePurchaseOrderCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeletePurchaseOrderCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeletePurchaseOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse.Fail("Purchase order not found.");

        // Only Draft or Cancelled POs may be deleted — anything past Approved preserves the audit trail
        if (po.Status != Domain.Entities.PurchaseOrderStatus.Draft &&
            po.Status != Domain.Entities.PurchaseOrderStatus.Cancelled)
        {
            return ApiResponse.Fail("Only draft or cancelled purchase orders can be deleted. Cancel it first.");
        }

        _repo.Remove(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Purchase order deleted.");
    }
}
