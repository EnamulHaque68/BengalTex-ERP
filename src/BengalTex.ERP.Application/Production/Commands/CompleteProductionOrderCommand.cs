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
    private readonly IRepository<Domain.Entities.JobCard, long> _jobCardRepo;
    private readonly IUnitOfWork _uow;
    private readonly IStockService _stock;
    private readonly IStockLotService _lots;
    private readonly IStockReservationService _reservations;
    private readonly IJournalPostingService _journal;
    private readonly ICostingRateResolver _rates;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IMediator _mediator;

    public CompleteProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.JobCard, long> jobCardRepo,
        IUnitOfWork uow,
        IStockService stock,
        IStockLotService lots,
        IStockReservationService reservations,
        IJournalPostingService journal,
        ICostingRateResolver rates,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IMediator mediator)
    {
        _repo = repo;
        _jobCardRepo = jobCardRepo;
        _uow = uow;
        _stock = stock;
        _lots = lots;
        _reservations = reservations;
        _journal = journal;
        _rates = rates;
        _currentUser = currentUser;
        _notifications = notifications;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        CompleteProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.RawMaterial)
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.ComponentProduct)
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
        // Polymorphic BOM lines: a line is either a raw material or a semi-finished
        // component product (sub-assembly), each checked against its own StockOnHand.
        var shortages = new List<string>();
        foreach (var bomLine in po.Bom.Lines)
        {
            var requiredQty = bomLine.Quantity * (1 + bomLine.WastagePercent / 100m) * scale;
            decimal available;
            string itemName;
            if (bomLine.RawMaterialId is int rmId)
            {
                available = await _stock.GetRawMaterialOnHandAsync(rmId, po.IssueWarehouseId, cancellationToken);
                itemName = bomLine.RawMaterial!.Name;
            }
            else
            {
                available = await _stock.GetProductOnHandAsync(bomLine.ComponentProductId!.Value, po.IssueWarehouseId, cancellationToken);
                itemName = bomLine.ComponentProduct!.Name;
            }
            if (available < requiredQty)
                shortages.Add($"{itemName}: need {requiredQty:0.####}, have {available:0.####}");
        }
        if (shortages.Count > 0)
        {
            return ApiResponse<ProductionOrderDto>.Fail(
                "Insufficient stock in issue warehouse — " + string.Join("; ", shortages));
        }

        // ── Release this run's soft reservations — the earmarked stock is now physically
        // consumed below, so it must stop counting against StockOnHand.ReservedQuantity. ──
        await _reservations.ReleaseForReferenceAsync("ProductionOrder", po.Id, cancellationToken);

        // ── Phase 2: post consumption movements per BOM line + accumulate consumed cost ──
        // rmCost is sourced from Raw Material Inventory; componentCost from Finished Goods
        // Inventory (sub-assemblies are produced finished goods) — the journal (Phase 5)
        // credits each from its own account.
        var movementDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var rmCost = 0m;
        var componentCost = 0m;
        foreach (var bomLine in po.Bom.Lines)
        {
            var consumedQty = bomLine.Quantity * (1 + bomLine.WastagePercent / 100m) * scale;
            if (bomLine.RawMaterialId is int rmId)
            {
                rmCost += consumedQty * bomLine.RawMaterial!.WeightedAverageCost;
                // FIFO lot draw-down — decrements the oldest lots and tags each issue movement;
                // any lot-less remainder posts un-tagged (same as before lot tracking existed).
                await _lots.ConsumeRawMaterialFifoAsync(
                    rawMaterialId: rmId,
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
            else
            {
                componentCost += consumedQty * bomLine.ComponentProduct!.WeightedAverageCost;
                await _stock.PostProductMovementAsync(
                    productId: bomLine.ComponentProductId!.Value,
                    warehouseId: po.IssueWarehouseId,
                    signedQuantity: -consumedQty,         // outbound (consumed into this production)
                    movementType: StockMovementType.ProductionIssue,
                    referenceType: "ProductionOrder",
                    referenceId: po.Id,
                    referenceCode: po.Code,
                    movementDate: movementDate,
                    notes: null,
                    ct: cancellationToken);
            }
        }
        var totalRmCost = rmCost + componentCost;   // total material cost rolled into the output FG

        // ── Phase A4: absorb conversion cost into WIP (labour + machine + factory OH) ──
        // Labour  = Σ over the order's job cards of ActiveMinutes × labour rate (per stage work center).
        // Machine = Σ (job cards with a machine) machine-hours × machine-OH rate.
        // FOH     = output quantity × factory-OH per-unit rate (global).
        // No configured rate → that layer is 0 → production stays material-only (pre-A4 behaviour).
        var jobCards = await _jobCardRepo.Query().AsNoTracking()
            .Where(j => j.ProductionOrderId == po.Id)
            .Select(j => new { j.ActiveMinutes, j.MachineId, WcId = j.ProductionStage != null ? j.ProductionStage.WorkCenterId : null })
            .ToListAsync(cancellationToken);

        decimal labourCost = 0m, machineCost = 0m;
        foreach (var jc in jobCards)
        {
            var mins = jc.ActiveMinutes ?? 0;
            if (mins <= 0) continue;
            var labourRate = await _rates.ResolveAsync(CostingRateType.Labour, movementDate, jc.WcId, cancellationToken);
            labourCost += mins * labourRate;
            if (jc.MachineId is not null)
            {
                var machineRate = await _rates.ResolveAsync(CostingRateType.MachineOH, movementDate, jc.WcId, cancellationToken);
                machineCost += (mins / 60m) * machineRate;
            }
        }
        var fohRate = await _rates.ResolveAsync(CostingRateType.FactoryOH, movementDate, null, cancellationToken);
        var overheadCost = po.Quantity * fohRate;

        labourCost = Math.Round(labourCost, 2, MidpointRounding.AwayFromZero);
        machineCost = Math.Round(machineCost, 2, MidpointRounding.AwayFromZero);
        overheadCost = Math.Round(overheadCost, 2, MidpointRounding.AwayFromZero);
        var loadedCost = totalRmCost + labourCost + machineCost + overheadCost;   // fully-loaded FG cost (F1)

        // ── Phase 3/A4: recompute Product WAC on the FULLY-LOADED cost, then post FG movement ──
        // FG unit cost = loaded cost ÷ produced qty (material + labour + machine + OH). Weighted-average
        // into the Product's existing stock value (qtyBefore captured before the receipt).
        if (po.Quantity > 0m)
        {
            var fgUnitCost = loadedCost / po.Quantity;
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

        // Phase 5 — Quality Hold (opt-in): keep the produced finished goods QC-held (soft-reserved in
        // the receive warehouse, so they can't be dispatched) until an explicit QC release.
        if (po.RequiresQc && po.Quantity > 0m)
        {
            await _reservations.ReserveProductAsync(
                po.ProductId, po.ReceiveWarehouseId, po.Quantity,
                "QcHold", po.Id, po.Code, cancellationToken);
        }

        // ── Phase 4: mark order completed ────────────────────────────────────────
        po.Status = ProductionOrderStatus.Completed;
        po.MaterialCost = totalRmCost;            // Phase 6 — persist consumed RM cost on the cost sheet
        po.LabourCost = labourCost;               // Phase A4 — GL-backed absorbed conversion cost
        po.MachineCost = machineCost;
        po.OverheadCost = overheadCost;
        po.ActualEndDate = movementDate;
        po.CompletedAt = DateTimeOffset.UtcNow;
        po.CompletedBy = _currentUser.UserName;
        _repo.Update(po);

        // ── Phase 5: auto-journals — backflush the RM cost through Work-In-Progress ──
        // Two distinct economic events posted at completion (backflush costing — appropriate for
        // short-cycle production that records issue + receipt together):
        //   (a) materials issued to WIP:  Dr WIP / Cr Raw Material Inventory (+ Cr Finished Goods for sub-assemblies)
        //   (b) WIP completed to finished goods:  Dr Finished Goods / Cr WIP
        // WIP nets to zero per run; its balance reflects only runs issued-but-not-yet-received.
        // (Only when consumed cost is non-zero — zero-cost components would produce zero-balance entries.
        //  Multi-level BOM: component sub-assemblies are issued out of Finished Goods Inventory, raw
        //  materials out of Raw Material Inventory — the credit splits accordingly; zero lines are dropped.)
        // Phase A3 — tag production legs with the order + style so WIP/output value is
        // attributable per production order and style.
        var prodDims = new Dimensions(StyleId: po.StyleId, ProductionOrderId: po.Id);

        // (a) materials issued to WIP (backflush) — only when material was consumed.
        if (totalRmCost > 0m)
        {
            await _journal.PostAsync(
                movementDate,
                $"Production {po.Code} — materials issued to WIP for {po.Product.Name}",
                "ProductionOrder", po.Id, po.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.WorkInProgressInventory, totalRmCost, 0m, prodDims),
                    new JournalPostingLine(LedgerAccounts.RawMaterialInventory, 0m, rmCost, prodDims),
                    new JournalPostingLine(LedgerAccounts.FinishedGoodsInventory, 0m, componentCost, prodDims),
                }, cancellationToken);
        }

        // (a2) Phase A4 — applied labour absorbed into WIP against the Applied Labour contra.
        if (labourCost > 0m)
        {
            await _journal.PostAsync(
                movementDate, $"Production {po.Code} — labour absorbed to WIP",
                "ProductionOrder", po.Id, po.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.WorkInProgressInventory, labourCost, 0m, prodDims),
                    new JournalPostingLine(LedgerAccounts.AppliedLabour, 0m, labourCost, prodDims),
                }, cancellationToken);
        }

        // (a3) Phase A4 — applied machine + factory overhead absorbed into WIP against Applied FOH contra.
        var appliedFoh = machineCost + overheadCost;
        if (appliedFoh > 0m)
        {
            await _journal.PostAsync(
                movementDate, $"Production {po.Code} — machine + factory overhead absorbed to WIP",
                "ProductionOrder", po.Id, po.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.WorkInProgressInventory, appliedFoh, 0m, prodDims),
                    new JournalPostingLine(LedgerAccounts.AppliedFactoryOverhead, 0m, appliedFoh, prodDims),
                }, cancellationToken);
        }

        // (b) WIP completed to finished goods at the FULLY-LOADED cost (material + conversion).
        if (loadedCost > 0m)
        {
            await _journal.PostAsync(
                movementDate,
                $"Production {po.Code} — WIP completed to finished goods ({po.Product.Name})",
                "ProductionOrder", po.Id, po.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.FinishedGoodsInventory, loadedCost, 0m, prodDims),
                    new JournalPostingLine(LedgerAccounts.WorkInProgressInventory, 0m, loadedCost, prodDims),
                }, cancellationToken);
        }

        // Phase 7 — smart notification: completed run is either QC-pending or finished-goods-ready.
        if (po.RequiresQc)
            await _notifications.NotifyAsync(
                NotificationChannels.InApp, NotificationRecipients.QcTeam,
                $"Production {po.Code} completed — QC pending",
                $"{po.Quantity:0.####} × {po.Product.Name} produced and QC-held. Inspect to release.",
                "ProductionOrder", po.Id, cancellationToken);
        else
            await _notifications.NotifyAsync(
                NotificationChannels.InApp, NotificationRecipients.SalesTeam,
                $"Production {po.Code} completed — finished goods ready",
                $"{po.Quantity:0.####} × {po.Product.Name} received into finished-goods stock.",
                "ProductionOrder", po.Id, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
