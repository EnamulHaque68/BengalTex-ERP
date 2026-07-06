using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.GoodsReceipt.Dtos;
using BengalTex.ERP.Application.GoodsReceipt.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.GoodsReceipt.Commands;

/// <summary>
/// Posts a draft GRN. Atomic in one SaveChanges:
///   1. Re-validate no line over-receives the remaining ordered quantity.
///   2. Increment <see cref="Domain.Entities.PurchaseOrderLine.ReceivedQuantity"/> per line.
///   3. Post a <see cref="StockMovement"/> per line (type = GrnReceipt) via <see cref="IStockService"/>,
///      which also upserts the <see cref="StockOnHand"/> snapshot.
///   4. Recompute parent PO status — all lines complete → Received; some &gt; 0 → PartiallyReceived.
///   5. Mark GRN as Posted with PostedAt/By.
/// Once Posted, the GRN is immutable.
/// </summary>
public sealed record PostGoodsReceiptCommand(long Id) : IRequest<ApiResponse<GoodsReceiptDto>>;

internal sealed class PostGoodsReceiptCommandHandler
    : IRequestHandler<PostGoodsReceiptCommand, ApiResponse<GoodsReceiptDto>>
{
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _repo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.StockLot, long> _lotRepo;
    private readonly IUnitOfWork _uow;
    private readonly IStockService _stock;
    private readonly IJournalPostingService _journal;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostGoodsReceiptCommandHandler(
        IRepository<Domain.Entities.GoodsReceiptNote, long> repo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.StockLot, long> lotRepo,
        IUnitOfWork uow,
        IStockService stock,
        IJournalPostingService journal,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _poRepo = poRepo;
        _lotRepo = lotRepo;
        _uow = uow;
        _stock = stock;
        _journal = journal;
        _numbering = numbering;
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

        // Re-validate over-receipt against current PO line state, apply increments,
        // and post one StockMovement per line via IStockService.
        var totalReceiptValue = 0m;   // Phase A2 — base-BDT value of goods received (for the GR/IR journal)
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

            // ── Weighted-average cost recompute (before the movement updates StockOnHand) ──
            // newWAC = (qtyBefore × oldWAC + receivedQty × poLinePrice) / (qtyBefore + receivedQty)
            var qtyBefore = await _stock.GetRawMaterialTotalOnHandAsync(poLine.RawMaterialId, cancellationToken);
            // WAC is held in base currency (BDT) — convert the PO-currency price via the PO's rate.
            var receivedCost = poLine.UnitPrice * po.ExchangeRate;
            totalReceiptValue += grnLine.ReceivedQuantity * receivedCost;   // Phase A2 — GR/IR value (same basis as WAC)
            var denom = qtyBefore + grnLine.ReceivedQuantity;
            if (denom > 0m)
            {
                poLine.RawMaterial.WeightedAverageCost =
                    (qtyBefore * poLine.RawMaterial.WeightedAverageCost + grnLine.ReceivedQuantity * receivedCost) / denom;
            }

            // ── Lot/batch capture — a line carrying a lot number births a traceable StockLot,
            //    and its GrnReceipt movement is tagged with that lot (FK resolves via navigation).
            Domain.Entities.StockLot? lot = null;
            if (!string.IsNullOrWhiteSpace(grnLine.LotNumber))
            {
                lot = new Domain.Entities.StockLot
                {
                    Code = await _numbering.NextAsync("LOT", null, cancellationToken),
                    LotNumber = grnLine.LotNumber!.Trim(),
                    RawMaterialId = poLine.RawMaterialId,
                    WarehouseId = grn.ReceivingWarehouseId,
                    SupplierId = po.SupplierId,
                    Shade = grnLine.Shade,
                    ReceivedDate = grn.ReceiveDate,
                    ManufactureDate = grnLine.ManufactureDate,
                    ExpiryDate = grnLine.ExpiryDate,
                    InitialQuantity = grnLine.ReceivedQuantity,
                    CurrentQuantity = grnLine.ReceivedQuantity,
                    Status = Domain.Entities.LotStatus.Active,
                    SourceType = "GRN",
                    SourceId = grn.Id,
                    SourceCode = grn.Code,
                    Notes = grnLine.LineNotes
                };
                await _lotRepo.AddAsync(lot, cancellationToken);
            }

            await _stock.PostRawMaterialMovementAsync(
                rawMaterialId: poLine.RawMaterialId,
                warehouseId: grn.ReceivingWarehouseId,
                signedQuantity: grnLine.ReceivedQuantity,         // inbound, positive
                movementType: StockMovementType.GrnReceipt,
                referenceType: "GRN",
                referenceId: grn.Id,
                referenceCode: grn.Code,
                movementDate: grn.ReceiveDate,
                notes: grnLine.LineNotes,
                ct: cancellationToken,
                lot: lot);
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
        grn.IsGlPosted = true;   // Phase A2 — this GRN carries a GR/IR credit; bills clear it (new path)

        // Phase A2 (F2) — inventory now hits the GL at the moment of receipt, matching the
        // stock/WAC update above. Value = Σ(received qty × PO unit price × PO rate), the same
        // basis the WAC recompute uses, so GL 1140 and perpetual stock stay in lock-step.
        //   Dr Raw Material Inventory / Cr GR/IR Clearing
        // The bill later clears GR/IR (Dr 2150 / Cr AP) instead of debiting inventory again.
        if (totalReceiptValue > 0m)
        {
            await _journal.PostAsync(
                grn.ReceiveDate, $"Goods received {grn.Code} (PO {po.Code})",
                "GoodsReceiptNote", grn.Id, grn.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.RawMaterialInventory, totalReceiptValue, 0m),
                    new JournalPostingLine(LedgerAccounts.GrIrClearing, 0m, totalReceiptValue),
                }, cancellationToken);
        }

        _repo.Update(grn);
        _poRepo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetGoodsReceiptByIdQuery(grn.Id), cancellationToken);
    }
}
