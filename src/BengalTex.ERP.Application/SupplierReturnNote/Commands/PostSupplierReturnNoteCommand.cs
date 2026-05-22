using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierReturnNote.Dtos;
using BengalTex.ERP.Application.SupplierReturnNote.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierReturnNote.Commands;

/// <summary>
/// Posts a Draft supplier return note. Two-pass atomic:
///   1. Validate-all — per line: ReturnedQty ≤ (GRN.ReceivedQty − GRN.ReturnedQty) AND
///      source warehouse has enough RawMaterial stock on hand. Collect violations,
///      fail entire post with consolidated message.
///   2. Apply-all — for each line:
///        - Increment <see cref="GoodsReceiptLine.ReturnedQuantity"/>
///        - Decrement <see cref="PurchaseOrderLine.ReceivedQuantity"/>
///        - Post a <c>ReturnOut</c> RawMaterial stock movement at the source warehouse via
///          <see cref="IStockService.PostRawMaterialMovementAsync"/>
///      Then recompute PO status (mirror of GRN Post logic): all-received → Received,
///      any-received → PartiallyReceived, all-zero → previous-non-receipt-state (Sent or
///      Approved). Closed/Cancelled POs are left untouched.
///   3. Flip SRN to Posted, set PostedAt/PostedBy.
///
/// PURELY INVENTORY — no financial side-effect on linked Supplier Invoice (per Phase 13 scope).
/// </summary>
public sealed record PostSupplierReturnNoteCommand(long Id) : IRequest<ApiResponse<SupplierReturnNoteDto>>;

internal sealed class PostSupplierReturnNoteCommandHandler
    : IRequestHandler<PostSupplierReturnNoteCommand, ApiResponse<SupplierReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.SupplierReturnNote, long> _repo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IStockService _stockService;
    private readonly IStockLotService _lots;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostSupplierReturnNoteCommandHandler(
        IRepository<Domain.Entities.SupplierReturnNote, long> repo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IStockService stockService,
        IStockLotService lots,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _grnRepo = grnRepo;
        _poRepo = poRepo;
        _stockService = stockService;
        _lots = lots;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierReturnNoteDto>> Handle(
        PostSupplierReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var srn = await _repo.Query()
            .Include(s => s.Lines).ThenInclude(l => l.RawMaterial)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);

        if (srn is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Supplier return note not found.");
        if (srn.Status != Domain.Entities.SupplierReturnNoteStatus.Draft)
            return ApiResponse<SupplierReturnNoteDto>.Fail("Only draft supplier return notes can be posted.");
        if (srn.Lines.Count == 0)
            return ApiResponse<SupplierReturnNoteDto>.Fail("Cannot post a supplier return note with no lines.");

        var grn = await _grnRepo.Query()
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == srn.GoodsReceiptNoteId, cancellationToken);
        if (grn is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Parent goods receipt note not found.");

        var po = await _poRepo.Query()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == grn.PurchaseOrderId, cancellationToken);
        if (po is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Parent purchase order not found.");

        var grnLineById = grn.Lines.ToDictionary(l => l.Id);

        // ─── Pass 1: validate qty bounds + source-warehouse stock availability ──
        var violations = new List<string>();
        foreach (var srnLine in srn.Lines)
        {
            if (!grnLineById.TryGetValue(srnLine.GoodsReceiptLineId, out var grnLine))
            {
                violations.Add($"GRN line {srnLine.GoodsReceiptLineId} not found on parent GRN {grn.Code}.");
                continue;
            }
            var availableForReturn = grnLine.ReceivedQuantity - grnLine.ReturnedQuantity;
            if (srnLine.ReturnedQuantity > availableForReturn)
            {
                violations.Add(
                    $"{srnLine.RawMaterial.Name}: return qty {srnLine.ReturnedQuantity:0.####} " +
                    $"exceeds available {availableForReturn:0.####}.");
                continue;
            }

            var onHand = await _stockService.GetRawMaterialOnHandAsync(
                srnLine.RawMaterialId, srn.ReturnFromWarehouseId, cancellationToken);
            if (srnLine.ReturnedQuantity > onHand)
            {
                violations.Add(
                    $"{srnLine.RawMaterial.Name}: insufficient stock at source warehouse " +
                    $"(need {srnLine.ReturnedQuantity:0.####}, have {onHand:0.####}).");
            }
        }
        if (violations.Count > 0)
            return ApiResponse<SupplierReturnNoteDto>.Fail("Cannot post SRN:\n" + string.Join("\n", violations));

        // ─── Pass 2: apply all (stock out + GRN line + PO line) ────────────
        var poLineById = po.Lines.ToDictionary(l => l.Id);
        foreach (var srnLine in srn.Lines)
        {
            var grnLine = grnLineById[srnLine.GoodsReceiptLineId];
            grnLine.ReturnedQuantity += srnLine.ReturnedQuantity;

            var poLine = poLineById[grnLine.PurchaseOrderLineId];
            poLine.ReceivedQuantity -= srnLine.ReturnedQuantity;

            // FIFO lot draw-down for the returned RM — decrements the oldest lots at the source
            // warehouse and tags each ReturnOut movement; lot-less remainder posts un-tagged.
            await _lots.ConsumeRawMaterialFifoAsync(
                rawMaterialId: srnLine.RawMaterialId,
                warehouseId: srn.ReturnFromWarehouseId,
                quantity: srnLine.ReturnedQuantity,              // positive; service posts outbound
                movementType: StockMovementType.ReturnOut,
                referenceType: "SRN",
                referenceId: srn.Id,
                referenceCode: srn.Code,
                movementDate: srn.ReturnDate,
                notes: srnLine.LineNotes,
                ct: cancellationToken);
        }

        // Recompute PO status — mirror of GRN Post logic, but only for non-terminal states.
        if (po.Status == Domain.Entities.PurchaseOrderStatus.Received
            || po.Status == Domain.Entities.PurchaseOrderStatus.PartiallyReceived
            || po.Status == Domain.Entities.PurchaseOrderStatus.Sent
            || po.Status == Domain.Entities.PurchaseOrderStatus.Approved)
        {
            var allComplete = po.Lines.All(l => l.ReceivedQuantity >= l.Quantity);
            var anyReceived = po.Lines.Any(l => l.ReceivedQuantity > 0);
            if (allComplete)
                po.Status = Domain.Entities.PurchaseOrderStatus.Received;
            else if (anyReceived)
                po.Status = Domain.Entities.PurchaseOrderStatus.PartiallyReceived;
            else
                // All received qty zeroed out — revert to Sent (last status before any receipt)
                po.Status = Domain.Entities.PurchaseOrderStatus.Sent;
        }

        srn.Status = Domain.Entities.SupplierReturnNoteStatus.Posted;
        srn.PostedAt = DateTimeOffset.UtcNow;
        srn.PostedBy = _currentUser.UserName;

        _repo.Update(srn);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierReturnNoteByIdQuery(srn.Id), cancellationToken);
    }
}
