using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.QcInspection.Dtos;
using BengalTex.ERP.Application.QcInspection.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QcInspection.Commands;

/// <summary>
/// Posts a Draft QC inspection. Two-pass atomic:
///   1. Validate-all — each line's RejectedQuantity ≤ current stock on hand at the source
///      warehouse. Collect violations, fail entire post with consolidated message.
///   2. Apply-all — for each line with RejectedQuantity > 0, move it out of the source
///      warehouse (QcRejectOut) and into the quarantine warehouse (QcRejectIn). Passed
///      quantity is untouched (stays usable in the source warehouse).
///   3. Compute OverallResult (all-passed → Passed, all-rejected → Failed, mixed →
///      PartiallyPassed), flip to Posted.
/// </summary>
public sealed record PostQcInspectionCommand(long Id) : IRequest<ApiResponse<QcInspectionDto>>;

internal sealed class PostQcInspectionCommandHandler
    : IRequestHandler<PostQcInspectionCommand, ApiResponse<QcInspectionDto>>
{
    private readonly IRepository<Domain.Entities.QcInspection, long> _repo;
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _prodRepo;
    private readonly IStockService _stock;
    private readonly IStockReservationService _reservations;
    private readonly IJournalPostingService _journal;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IMediator _mediator;

    public PostQcInspectionCommandHandler(
        IRepository<Domain.Entities.QcInspection, long> repo,
        IRepository<Domain.Entities.ProductionOrder, long> prodRepo,
        IStockService stock,
        IStockReservationService reservations,
        IJournalPostingService journal,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IMediator mediator)
    {
        _repo = repo;
        _prodRepo = prodRepo;
        _stock = stock;
        _reservations = reservations;
        _journal = journal;
        _uow = uow;
        _currentUser = currentUser;
        _notifications = notifications;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QcInspectionDto>> Handle(
        PostQcInspectionCommand cmd, CancellationToken cancellationToken)
    {
        var insp = await _repo.Query()
            .Include(q => q.Lines).ThenInclude(l => l.RawMaterial)
            .Include(q => q.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(q => q.Id == cmd.Id, cancellationToken);

        if (insp is null) return ApiResponse<QcInspectionDto>.Fail("QC inspection not found.");
        if (insp.Status != Domain.Entities.QcInspectionStatus.Draft)
            return ApiResponse<QcInspectionDto>.Fail("Only draft QC inspections can be posted.");
        if (insp.Lines.Count == 0)
            return ApiResponse<QcInspectionDto>.Fail("Cannot post a QC inspection with no lines.");

        // ─── Pass 1: validate rejected qty against source stock ─────────────
        var violations = new List<string>();
        foreach (var line in insp.Lines.Where(l => l.RejectedQuantity > 0m))
        {
            decimal available;
            string itemLabel;
            if (line.RawMaterialId.HasValue)
            {
                available = await _stock.GetRawMaterialOnHandAsync(
                    line.RawMaterialId.Value, insp.InspectedFromWarehouseId, cancellationToken);
                itemLabel = line.RawMaterial?.Name ?? $"RM {line.RawMaterialId}";
            }
            else
            {
                available = await _stock.GetProductOnHandAsync(
                    line.ProductId!.Value, insp.InspectedFromWarehouseId, cancellationToken);
                itemLabel = line.Product?.Name ?? $"Product {line.ProductId}";
            }

            if (line.RejectedQuantity > available)
            {
                violations.Add(
                    $"{itemLabel}: rejected {line.RejectedQuantity:0.####} exceeds available " +
                    $"{available:0.####} at source warehouse.");
            }
        }
        if (violations.Count > 0)
            return ApiResponse<QcInspectionDto>.Fail("Cannot post QC inspection:\n" + string.Join("\n", violations));

        // ─── QC-hold (Phase 5 upgrade): a finished-goods inspection of a QC-held production releases
        // the inspected qty (passed + rejected) from the hold — passed becomes usable, rejected leaves.
        // Guard: cannot inspect more than is currently held. ───────────────────
        Domain.Entities.ProductionOrder? heldPo = null;
        var heldRemaining = 0m;
        if (insp.SourceType == Domain.Entities.QcSourceType.FinishedGoods && insp.ProductionOrderId is long poId)
        {
            heldPo = await _prodRepo.GetByIdAsync(poId, cancellationToken);
            if (heldPo?.RequiresQc == true)
            {
                heldRemaining = await _reservations.GetReservedForReferenceAsync("QcHold", poId, cancellationToken);
                var inspectedTotal = insp.Lines.Sum(l => l.InspectedQuantity);
                if (heldRemaining > 0m && inspectedTotal > heldRemaining + 0.0001m)
                    return ApiResponse<QcInspectionDto>.Fail(
                        $"Inspecting {inspectedTotal:0.####} exceeds the QC-held quantity ({heldRemaining:0.####}). " +
                        "Inspect at most the held quantity.");
            }
        }

        // ─── Pass 2: route rejected qty per disposition (Quarantine/Reject/Rework → destination wh;
        // Scrap → write-off). Default (null) = Quarantine (legacy behaviour). ────
        var disposition = insp.RejectDisposition ?? Domain.Entities.QcRejectDisposition.Quarantine;
        var isScrap = disposition == Domain.Entities.QcRejectDisposition.Scrap;
        var rmScrapCost = 0m;
        var fgScrapCost = 0m;

        foreach (var line in insp.Lines.Where(l => l.RejectedQuantity > 0m))
        {
            if (isScrap)
            {
                if (line.RawMaterialId.HasValue)
                {
                    rmScrapCost += line.RejectedQuantity * (line.RawMaterial?.WeightedAverageCost ?? 0m);
                    await _stock.PostRawMaterialMovementAsync(
                        line.RawMaterialId.Value, insp.InspectedFromWarehouseId, -line.RejectedQuantity,
                        StockMovementType.Scrap, "QcInspection", insp.Id, insp.Code,
                        insp.InspectionDate, line.DefectNotes, cancellationToken);
                }
                else
                {
                    fgScrapCost += line.RejectedQuantity * (line.Product?.WeightedAverageCost ?? 0m);
                    await _stock.PostProductMovementAsync(
                        line.ProductId!.Value, insp.InspectedFromWarehouseId, -line.RejectedQuantity,
                        StockMovementType.Scrap, "QcInspection", insp.Id, insp.Code,
                        insp.InspectionDate, line.DefectNotes, cancellationToken);
                }
            }
            else if (line.RawMaterialId.HasValue)
            {
                await _stock.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, insp.InspectedFromWarehouseId, -line.RejectedQuantity,
                    StockMovementType.QcRejectOut, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
                await _stock.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, insp.QuarantineWarehouseId, line.RejectedQuantity,
                    StockMovementType.QcRejectIn, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
            }
            else
            {
                await _stock.PostProductMovementAsync(
                    line.ProductId!.Value, insp.InspectedFromWarehouseId, -line.RejectedQuantity,
                    StockMovementType.QcRejectOut, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
                await _stock.PostProductMovementAsync(
                    line.ProductId.Value, insp.QuarantineWarehouseId, line.RejectedQuantity,
                    StockMovementType.QcRejectIn, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
            }
        }

        // Scrap write-off journal (WAC): Dr Material Wastage / Cr Inventory (RM and/or FG).
        if (rmScrapCost + fgScrapCost > 0m)
        {
            var journalLines = new List<JournalPostingLine>
            {
                new(LedgerAccounts.MaterialWastage, rmScrapCost + fgScrapCost, 0m),
            };
            if (rmScrapCost > 0m) journalLines.Add(new(LedgerAccounts.RawMaterialInventory, 0m, rmScrapCost));
            if (fgScrapCost > 0m) journalLines.Add(new(LedgerAccounts.FinishedGoodsInventory, 0m, fgScrapCost));
            await _journal.PostAsync(
                insp.InspectionDate, $"QC scrap {insp.Code} — write-off at WAC",
                "QcInspection", insp.Id, insp.Code, journalLines, cancellationToken);
        }

        // Release the inspected qty from the QC hold (passed becomes usable; rejected already moved out).
        if (heldPo?.RequiresQc == true && heldRemaining > 0m)
        {
            var inspectedTotal = insp.Lines.Sum(l => l.InspectedQuantity);
            var releaseQty = Math.Min(inspectedTotal, heldRemaining);
            await _reservations.ReleaseQuantityAsync("QcHold", heldPo.Id, releaseQty, cancellationToken);
            if (releaseQty >= heldRemaining - 0.0001m)   // hold fully cleared
            {
                heldPo.QcReleasedAt = DateTimeOffset.UtcNow;
                _prodRepo.Update(heldPo);
            }
        }

        // Overall result
        var totalRejected = insp.Lines.Sum(l => l.RejectedQuantity);
        var totalPassed = insp.Lines.Sum(l => l.PassedQuantity);
        insp.OverallResult = totalRejected == 0m
            ? Domain.Entities.QcResult.Passed
            : (totalPassed == 0m ? Domain.Entities.QcResult.Failed : Domain.Entities.QcResult.PartiallyPassed);

        insp.Status = Domain.Entities.QcInspectionStatus.Posted;
        insp.InspectedBy ??= _currentUser.UserName;

        _repo.Update(insp);

        // Phase 7 — smart notification: QC result (passed FG now released / usable).
        if (insp.SourceType == Domain.Entities.QcSourceType.FinishedGoods)
            await _notifications.NotifyAsync(
                NotificationChannels.InApp, NotificationRecipients.SalesTeam,
                $"QC {insp.Code} posted — {insp.OverallResult}",
                $"{totalPassed:0.####} passed, {totalRejected:0.####} rejected. Passed finished goods are now available.",
                "QcInspection", insp.Id, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQcInspectionByIdQuery(insp.Id), cancellationToken);
    }
}
