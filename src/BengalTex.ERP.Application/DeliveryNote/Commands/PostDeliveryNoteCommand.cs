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
///   4. Recompute parent SO status — all lines complete → Dispatched; some &gt; 0 →
///      PartiallyDispatched.
///   5. Mark DN as Posted with PostedAt/By.
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
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostDeliveryNoteCommandHandler(
        IRepository<Domain.Entities.DeliveryNote, long> repo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IUnitOfWork uow,
        IStockService stock,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _soRepo = soRepo;
        _uow = uow;
        _stock = stock;
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

            var available = await _stock.GetProductOnHandAsync(
                soLine.ProductId, dn.DispatchWarehouseId, cancellationToken);
            if (dnLine.DispatchedQuantity > available)
                return ApiResponse<DeliveryNoteDto>.Fail(
                    $"{soLine.Product.Name}: insufficient stock in dispatch warehouse " +
                    $"(need {dnLine.DispatchedQuantity:0.####}, have {available:0.####}).");
        }

        // Pass 2 — apply: increment SO lines + post movements
        foreach (var dnLine in dn.Lines)
        {
            var soLine = so.Lines.First(sl => sl.Id == dnLine.SalesOrderLineId);
            soLine.DispatchedQuantity += dnLine.DispatchedQuantity;

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
