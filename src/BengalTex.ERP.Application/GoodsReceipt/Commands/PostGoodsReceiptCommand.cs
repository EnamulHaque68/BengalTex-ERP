using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.GoodsReceipt.Dtos;
using BengalTex.ERP.Application.GoodsReceipt.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.GoodsReceipt.Commands;

/// <summary>
/// Posts a draft GRN. Atomic in one SaveChanges:
///   1. Re-validate no line over-receives the remaining ordered quantity.
///   2. Increment <see cref="Domain.Entities.PurchaseOrderLine.ReceivedQuantity"/> per line.
///   3. Recompute parent PO status — all lines complete → Received; some &gt; 0 → PartiallyReceived.
///   4. Mark GRN as Posted with PostedAt/By.
/// Once Posted, the GRN is immutable.
/// </summary>
public sealed record PostGoodsReceiptCommand(long Id) : IRequest<ApiResponse<GoodsReceiptDto>>;

internal sealed class PostGoodsReceiptCommandHandler
    : IRequestHandler<PostGoodsReceiptCommand, ApiResponse<GoodsReceiptDto>>
{
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _repo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostGoodsReceiptCommandHandler(
        IRepository<Domain.Entities.GoodsReceiptNote, long> repo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _poRepo = poRepo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<GoodsReceiptDto>> Handle(
        PostGoodsReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var grn = await _repo.Query()
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == cmd.Id, cancellationToken);
        if (grn is null) return ApiResponse<GoodsReceiptDto>.Fail("Goods receipt not found.");
        if (grn.Status != Domain.Entities.GoodsReceiptStatus.Draft)
            return ApiResponse<GoodsReceiptDto>.Fail("Only draft goods receipts can be posted.");
        if (grn.Lines.Count == 0)
            return ApiResponse<GoodsReceiptDto>.Fail("Cannot post a goods receipt with no lines.");

        // Load PO + lines (tracked) so increments persist via SaveChanges
        var po = await _poRepo.Query()
            .Include(p => p.Lines).ThenInclude(pl => pl.RawMaterial)
            .FirstOrDefaultAsync(p => p.Id == grn.PurchaseOrderId, cancellationToken);
        if (po is null) return ApiResponse<GoodsReceiptDto>.Fail("Parent purchase order not found.");

        if (po.Status != Domain.Entities.PurchaseOrderStatus.Approved &&
            po.Status != Domain.Entities.PurchaseOrderStatus.Sent &&
            po.Status != Domain.Entities.PurchaseOrderStatus.PartiallyReceived)
        {
            return ApiResponse<GoodsReceiptDto>.Fail(
                "PO is not in a receivable state — cannot post receipt.");
        }

        // Re-validate over-receipt against current PO line state, then apply increments
        foreach (var grnLine in grn.Lines)
        {
            var poLine = po.Lines.FirstOrDefault(pl => pl.Id == grnLine.PurchaseOrderLineId);
            if (poLine is null)
                return ApiResponse<GoodsReceiptDto>.Fail(
                    $"PO line {grnLine.PurchaseOrderLineId} not found on PO {po.Code}.");

            var remaining = poLine.Quantity - poLine.ReceivedQuantity;
            if (grnLine.ReceivedQuantity > remaining)
                return ApiResponse<GoodsReceiptDto>.Fail(
                    $"{poLine.RawMaterial.Name}: would exceed ordered qty " +
                    $"({remaining:0.####} remaining).");

            poLine.ReceivedQuantity += grnLine.ReceivedQuantity;
        }

        // Recompute PO status
        var allComplete = po.Lines.All(pl => pl.ReceivedQuantity >= pl.Quantity);
        var anyReceived = po.Lines.Any(pl => pl.ReceivedQuantity > 0);
        if (allComplete)
            po.Status = Domain.Entities.PurchaseOrderStatus.Received;
        else if (anyReceived)
            po.Status = Domain.Entities.PurchaseOrderStatus.PartiallyReceived;

        // Mark GRN posted
        grn.Status = Domain.Entities.GoodsReceiptStatus.Posted;
        grn.PostedAt = DateTimeOffset.UtcNow;
        grn.PostedBy = _currentUser.UserName;

        _repo.Update(grn);
        _poRepo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetGoodsReceiptByIdQuery(grn.Id), cancellationToken);
    }
}
