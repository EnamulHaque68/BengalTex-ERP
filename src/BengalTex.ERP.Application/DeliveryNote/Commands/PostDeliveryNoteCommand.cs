using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.DeliveryNote.Dtos;
using BengalTex.ERP.Application.DeliveryNote.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.DeliveryNote.Commands;

/// <summary>
/// Posts a draft DN. Atomic in one SaveChanges:
///   1. Re-validate per line: no over-dispatch against the remaining ordered qty, and
///      sufficient Product stock in the dispatch warehouse. Any short → Fail.
///   2. Increment <see cref="SalesOrderLine.DispatchedQuantity"/> per line.
///   3. Post a <see cref="StockMovement"/> per line (type = SalesDispatch, − qty) via
///      <see cref="IStockService.PostProductMovementAsync"/>.
///   4. Auto-journal COGS at dispatch (perpetual): Dr Cost of Goods Sold / Cr Finished
///      Goods Inventory at Σ(qty × Product.WeightedAverageCost) — pairs with the revenue
///      entry made by Customer Invoice Issue, so the P&amp;L shows margin per period.
///   5. Recompute parent SO status — all lines complete → Dispatched; some &gt; 0 →
///      PartiallyDispatched.
///   6. Mark DN as Posted with PostedAt/By.
/// Once Posted, the DN is immutable.
/// </summary>
public sealed record PostDeliveryNoteCommand(long Id) : IRequest<ApiResponse<DeliveryNoteDto>>;

internal sealed class PostDeliveryNoteCommandHandler
    : IRequestHandler<PostDeliveryNoteCommand, ApiResponse<DeliveryNoteDto>>
{
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _repo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IUnitOfWork _uow;
    private readonly IStockService _stock;
    private readonly IStockReservationService _reservations;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostDeliveryNoteCommandHandler(
        IRepository<Domain.Entities.DeliveryNote, long> repo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IUnitOfWork uow,
        IStockService stock,
        IStockReservationService reservations,
        IJournalPostingService journal,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _soRepo = soRepo;
        _uow = uow;
        _stock = stock;
        _reservations = reservations;
        _journal = journal;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<DeliveryNoteDto>> Handle(
        PostDeliveryNoteCommand cmd, CancellationToken cancellationToken)
    {
        var dn = await _repo.Query()
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == cmd.Id, cancellationToken);
        if (dn is null) return ApiResponse<DeliveryNoteDto>.Fail("Delivery note not found.");
        if (dn.Status != Domain.Entities.DeliveryNoteStatus.Draft)
            return ApiResponse<DeliveryNoteDto>.Fail("Only draft delivery notes can be posted.");
        if (dn.Lines.Count == 0)
            return ApiResponse<DeliveryNoteDto>.Fail("Cannot post a delivery note with no lines.");

        // Load SO + lines (tracked) so increments persist via SaveChanges
        var so = await _soRepo.Query()
            .Include(s => s.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(s => s.Id == dn.SalesOrderId, cancellationToken);
        if (so is null) return ApiResponse<DeliveryNoteDto>.Fail("Parent sales order not found.");

        if (so.Status != Domain.Entities.SalesOrderStatus.Confirmed &&
            so.Status != Domain.Entities.SalesOrderStatus.PartiallyDispatched)
        {
            return ApiResponse<DeliveryNoteDto>.Fail(
                "SO is not in a dispatchable state — cannot post delivery.");
        }

        // Pass 1 — validate over-dispatch + stock availability for every line
        foreach (var dnLine in dn.Lines)
        {
            var soLine = so.Lines.FirstOrDefault(sl => sl.Id == dnLine.SalesOrderLineId);
            if (soLine is null)
                return ApiResponse<DeliveryNoteDto>.Fail(
                    $"SO line {dnLine.SalesOrderLineId} not found on SO {so.Code}.");

            var remaining = soLine.Quantity - soLine.DispatchedQuantity;
            if (dnLine.DispatchedQuantity > remaining)
                return ApiResponse<DeliveryNoteDto>.Fail(
                    $"{soLine.Product.Name}: would exceed ordered qty " +
                    $"({remaining:0.####} remaining).");

            // Available = on hand − reserved (Phase 2/5): excludes QC-held / earmarked finished goods.
            var onHand = await _stock.GetProductOnHandAsync(
                soLine.ProductId, dn.DispatchWarehouseId, cancellationToken);
            var reserved = await _reservations.GetReservedProductAsync(
                soLine.ProductId, dn.DispatchWarehouseId, cancellationToken);
            var available = onHand - reserved;
            if (dnLine.DispatchedQuantity > available)
                return ApiResponse<DeliveryNoteDto>.Fail(
                    $"{soLine.Product.Name}: insufficient available stock in dispatch warehouse " +
                    $"(need {dnLine.DispatchedQuantity:0.####}, available {available:0.####}" +
                    (reserved > 0m ? $", of which {reserved:0.####} is reserved/QC-held" : "") + ").");
        }

        // Pass 2 — apply: increment SO lines + post movements; build per-line COGS legs (Phase A3).
        // COGS is split per line so each Dr COGS carries its buyer/style/order dimensions; the
        // balancing Cr FG stays aggregate (a balance-sheet account — no per-style analysis needed).
        var cogsLegs = new List<JournalPostingLine>();
        var totalCogs = 0m;
        foreach (var dnLine in dn.Lines)
        {
            var soLine = so.Lines.First(sl => sl.Id == dnLine.SalesOrderLineId);
            soLine.DispatchedQuantity += dnLine.DispatchedQuantity;

            var lineCogs = Math.Round(dnLine.DispatchedQuantity * soLine.Product.WeightedAverageCost, 2, MidpointRounding.AwayFromZero);
            if (lineCogs > 0m)
            {
                cogsLegs.Add(new JournalPostingLine(LedgerAccounts.CostOfGoodsSold, lineCogs, 0m,
                    new Dimensions(BuyerId: so.CustomerId, StyleId: soLine.StyleId, SalesOrderId: so.Id)));
                totalCogs += lineCogs;
            }

            await _stock.PostProductMovementAsync(
                productId: soLine.ProductId,
                warehouseId: dn.DispatchWarehouseId,
                signedQuantity: -dnLine.DispatchedQuantity,         // outbound
                movementType: StockMovementType.SalesDispatch,
                referenceType: "DN",
                referenceId: dn.Id,
                referenceCode: dn.Code,
                movementDate: dn.DispatchDate,
                notes: dnLine.LineNotes,
                ct: cancellationToken);
        }

        // Auto-journal — recognize cost of goods at dispatch (zero-cost FG → no entry).
        if (totalCogs > 0m)
        {
            cogsLegs.Add(new JournalPostingLine(LedgerAccounts.FinishedGoodsInventory, 0m, totalCogs));
            await _journal.PostAsync(
                dn.DispatchDate, $"COGS on dispatch {dn.Code} (SO {so.Code})",
                "DeliveryNote", dn.Id, dn.Code,
                cogsLegs, cancellationToken);
        }

        // Recompute SO status
        var allComplete = so.Lines.All(l => l.DispatchedQuantity >= l.Quantity);
        var anyDispatched = so.Lines.Any(l => l.DispatchedQuantity > 0);
        if (allComplete)
            so.Status = Domain.Entities.SalesOrderStatus.Dispatched;
        else if (anyDispatched)
            so.Status = Domain.Entities.SalesOrderStatus.PartiallyDispatched;

        // Mark DN posted
        dn.Status = Domain.Entities.DeliveryNoteStatus.Posted;
        dn.PostedAt = DateTimeOffset.UtcNow;
        dn.PostedBy = _currentUser.UserName;

        _repo.Update(dn);
        _soRepo.Update(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetDeliveryNoteByIdQuery(dn.Id), cancellationToken);
    }
}
