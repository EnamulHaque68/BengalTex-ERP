using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>
/// Completes an in-progress production. Atomic in one SaveChanges:
///   1. Stock validation — for each BOM line, scaled consumption ≤ current StockOnHand
///      in the issue warehouse. Any short → <c>Fail</c> with details.
///   2. Post a <see cref="StockMovementType.ProductionIssue"/> movement per BOM line
///      (RM out of the issue warehouse, negative qty) via <see cref="IStockService"/>.
///   3. Post a <see cref="StockMovementType.ProductionReceipt"/> movement for the
///      output Product (into the receive warehouse, positive qty).
///   4. Set status Completed + ActualEndDate + CompletedAt/By.
/// </summary>
public sealed record CompleteProductionOrderCommand(long Id) : IRequest<ApiResponse<ProductionOrderDto>>;

internal sealed class CompleteProductionOrderCommandHandler
    : IRequestHandler<CompleteProductionOrderCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStockService _stock;
    private readonly IStockLotService _lots;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public CompleteProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IUnitOfWork uow,
        IStockService stock,
        IStockLotService lots,
        IJournalPostingService journal,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _stock = stock;
        _lots = lots;
        _journal = journal;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        CompleteProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.RawMaterial)
            .Include(p => p.Product)
            .Include(p => p.Stages)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, cancellationToken);

        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");
        if (po.Status != ProductionOrderStatus.InProgress)
            return ApiResponse<ProductionOrderDto>.Fail("Only in-progress production orders can be completed.");

        // Multi-stage gate (additive): if a routing exists, every stage must be done first.
        if (po.Stages.Count > 0 && po.Stages.Any(s =>
                s.Status != ProductionStageStatus.Completed && s.Status != ProductionStageStatus.Skipped))
        {
            var pending = po.Stages
                .Where(s => s.Status != ProductionStageStatus.Completed && s.Status != ProductionStageStatus.Skipped)
                .OrderBy(s => s.Sequence)
                .Select(s => s.StageName);
            return ApiResponse<ProductionOrderDto>.Fail(
                "Complete or skip all production stages first — pending: " + string.Join(", ", pending));
        }
        if (po.Bom.Lines.Count == 0)
            return ApiResponse<ProductionOrderDto>.Fail("Cannot complete a production whose BOM has no lines.");
        if (po.Bom.OutputQuantity <= 0)
            return ApiResponse<ProductionOrderDto>.Fail("BOM output quantity must be greater than zero.");

        var scale = po.Quantity / po.Bom.OutputQuantity;

        // ── Phase 1: stock-availability pre-check (block on any shortage) ────────
        var shortages = new List<string>();
        foreach (var bomLine in po.Bom.Lines)
        {
            var requiredQty = bomLine.Quantity * (1 + bomLine.WastagePercent / 100m) * scale;
            var available = await _stock.GetRawMaterialOnHandAsync(
                bomLine.RawMaterialId, po.IssueWarehouseId, cancellationToken);
            if (available < requiredQty)
            {
                shortages.Add(
                    $"{bomLine.RawMaterial.Name}: need {requiredQty:0.####}, have {available:0.####}");
            }
        }
        if (shortages.Count > 0)
        {
            return ApiResponse<ProductionOrderDto>.Fail(
                "Insufficient stock in issue warehouse — " + string.Join("; ", shortages));
        }

        // ── Phase 2: post RM-out movements per BOM line + accumulate consumed cost ──
        var movementDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalRmCost = 0m;
        foreach (var bomLine in po.Bom.Lines)
        {
            var consumedQty = bomLine.Quantity * (1 + bomLine.WastagePercent / 100m) * scale;
            totalRmCost += consumedQty * bomLine.RawMaterial.WeightedAverageCost;
            // FIFO lot draw-down — decrements the oldest lots and tags each issue movement;
            // any lot-less remainder posts un-tagged (same as before lot tracking existed).
            await _lots.ConsumeRawMaterialFifoAsync(
                rawMaterialId: bomLine.RawMaterialId,
                warehouseId: po.IssueWarehouseId,
                quantity: consumedQty,                // positive amount; service posts outbound
                movementType: StockMovementType.ProductionIssue,
                referenceType: "ProductionOrder",
                referenceId: po.Id,
                referenceCode: po.Code,
                movementDate: movementDate,
                notes: null,
                ct: cancellationToken);
        }

        // ── Phase 3: recompute Product WAC, then post finished-goods movement ────
        // FG unit cost = total RM cost consumed ÷ produced qty. Weighted-average it into
        // the Product's existing stock value (qtyBefore captured before the receipt).
        if (po.Quantity > 0m)
        {
            var fgUnitCost = totalRmCost / po.Quantity;
            var productQtyBefore = await _stock.GetProductTotalOnHandAsync(po.ProductId, cancellationToken);
            var denom = productQtyBefore + po.Quantity;
            if (denom > 0m)
            {
                po.Product.WeightedAverageCost =
                    (productQtyBefore * po.Product.WeightedAverageCost + po.Quantity * fgUnitCost) / denom;
            }
        }

        await _stock.PostProductMovementAsync(
            productId: po.ProductId,
            warehouseId: po.ReceiveWarehouseId,
            signedQuantity: po.Quantity,              // inbound
            movementType: StockMovementType.ProductionReceipt,
            referenceType: "ProductionOrder",
            referenceId: po.Id,
            referenceCode: po.Code,
            movementDate: movementDate,
            notes: null,
            ct: cancellationToken);

        // ── Phase 4: mark order completed ────────────────────────────────────────
        po.Status = ProductionOrderStatus.Completed;
        po.ActualEndDate = movementDate;
        po.CompletedAt = DateTimeOffset.UtcNow;
        po.CompletedBy = _currentUser.UserName;
        _repo.Update(po);

        // ── Phase 5: auto-journals — backflush the RM cost through Work-In-Progress ──
        // Two distinct economic events posted at completion (backflush costing — appropriate for
        // short-cycle production that records issue + receipt together):
        //   (a) materials issued to WIP:  Dr WIP / Cr Raw Material Inventory
        //   (b) WIP completed to finished goods:  Dr Finished Goods / Cr WIP
        // WIP nets to zero per run; its balance reflects only runs issued-but-not-yet-received.
        // (Only when consumed cost is non-zero — zero-cost RMs would produce zero-balance entries.)
        if (totalRmCost > 0m)
        {
            await _journal.PostAsync(
                movementDate,
                $"Production {po.Code} — raw materials issued to WIP for {po.Product.Name}",
                "ProductionOrder", po.Id, po.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.WorkInProgressInventory, totalRmCost, 0m),
                    new JournalPostingLine(LedgerAccounts.RawMaterialInventory, 0m, totalRmCost),
                }, cancellationToken);

            await _journal.PostAsync(
                movementDate,
                $"Production {po.Code} — WIP completed to finished goods ({po.Product.Name})",
                "ProductionOrder", po.Id, po.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.FinishedGoodsInventory, totalRmCost, 0m),
                    new JournalPostingLine(LedgerAccounts.WorkInProgressInventory, 0m, totalRmCost),
                }, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
