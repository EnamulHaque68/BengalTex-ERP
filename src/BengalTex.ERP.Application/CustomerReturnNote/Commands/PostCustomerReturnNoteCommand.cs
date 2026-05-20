using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.CustomerReturnNote.Dtos;
using BengalTex.ERP.Application.CustomerReturnNote.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerReturnNote.Commands;

/// <summary>
/// Posts a Draft customer return note. Two-pass atomic:
///   1. Validate-all — each line's ReturnedQty ≤ (DN.DispatchedQty − DN.ReturnedQty).
///      Collect all violations, fail entire post with consolidated message if any.
///   2. Apply-all — for each line:
///        - Increment <see cref="DeliveryNoteLine.ReturnedQuantity"/>
///        - Decrement <see cref="SalesOrderLine.DispatchedQuantity"/> (re-opens the SO line)
///        - Post a <c>ReturnIn</c> Product stock movement at the return warehouse via
///          <see cref="IStockService.PostProductMovementAsync"/>
///      Then recompute SO status (mirror of DN Post logic): all-dispatched → Dispatched,
///      any-dispatched → PartiallyDispatched, all-zero → Confirmed. SO statuses that are
///      Closed/Cancelled/Delivered are left untouched (terminal states).
///   3. Flip CRN to Posted, set PostedAt/PostedBy.
///
/// PURELY INVENTORY — no financial side-effect on linked Customer Invoice (per Phase 13 scope).
/// </summary>
public sealed record PostCustomerReturnNoteCommand(long Id) : IRequest<ApiResponse<CustomerReturnNoteDto>>;

internal sealed class PostCustomerReturnNoteCommandHandler
    : IRequestHandler<PostCustomerReturnNoteCommand, ApiResponse<CustomerReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.CustomerReturnNote, long> _repo;
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _dnRepo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostCustomerReturnNoteCommandHandler(
        IRepository<Domain.Entities.CustomerReturnNote, long> repo,
        IRepository<Domain.Entities.DeliveryNote, long> dnRepo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IStockService stockService,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _dnRepo = dnRepo;
        _soRepo = soRepo;
        _stockService = stockService;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerReturnNoteDto>> Handle(
        PostCustomerReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var crn = await _repo.Query()
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);

        if (crn is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Customer return note not found.");
        if (crn.Status != Domain.Entities.CustomerReturnNoteStatus.Draft)
            return ApiResponse<CustomerReturnNoteDto>.Fail("Only draft customer return notes can be posted.");
        if (crn.Lines.Count == 0)
            return ApiResponse<CustomerReturnNoteDto>.Fail("Cannot post a customer return note with no lines.");

        // Load the parent DN with lines (tracked — we'll mutate ReturnedQuantity)
        var dn = await _dnRepo.Query()
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == crn.DeliveryNoteId, cancellationToken);
        if (dn is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Parent delivery note not found.");

        // Load the parent SO with lines (we mutate DispatchedQuantity + Status)
        var so = await _soRepo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == dn.SalesOrderId, cancellationToken);
        if (so is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Parent sales order not found.");

        var dnLineById = dn.Lines.ToDictionary(l => l.Id);

        // ─── Pass 1: validate all lines ─────────────────────────────────────
        var violations = new List<string>();
        foreach (var crnLine in crn.Lines)
        {
            if (!dnLineById.TryGetValue(crnLine.DeliveryNoteLineId, out var dnLine))
            {
                violations.Add($"DN line {crnLine.DeliveryNoteLineId} not found on parent DN {dn.Code}.");
                continue;
            }
            var available = dnLine.DispatchedQuantity - dnLine.ReturnedQuantity;
            if (crnLine.ReturnedQuantity > available)
            {
                violations.Add(
                    $"{crnLine.Product.Name}: return qty {crnLine.ReturnedQuantity:0.####} " +
                    $"exceeds available {available:0.####}.");
            }
        }
        if (violations.Count > 0)
            return ApiResponse<CustomerReturnNoteDto>.Fail("Cannot post CRN:\n" + string.Join("\n", violations));

        // ─── Pass 2: apply all (stock + DN line + SO line) ──────────────────
        var soLineById = so.Lines.ToDictionary(l => l.Id);
        foreach (var crnLine in crn.Lines)
        {
            var dnLine = dnLineById[crnLine.DeliveryNoteLineId];
            dnLine.ReturnedQuantity += crnLine.ReturnedQuantity;

            var soLine = soLineById[dnLine.SalesOrderLineId];
            soLine.DispatchedQuantity -= crnLine.ReturnedQuantity;

            await _stockService.PostProductMovementAsync(
                productId: crnLine.ProductId,
                warehouseId: crn.ReturnWarehouseId,
                signedQuantity: crnLine.ReturnedQuantity,        // inbound
                movementType: StockMovementType.ReturnIn,
                referenceType: "CRN",
                referenceId: crn.Id,
                referenceCode: crn.Code,
                movementDate: crn.ReturnDate,
                notes: crnLine.LineNotes,
                ct: cancellationToken);
        }

        // Recompute SO status — mirror of DN Post logic, but only when SO is in a
        // non-terminal dispatch-related state. Closed/Cancelled/Delivered are left alone.
        if (so.Status == Domain.Entities.SalesOrderStatus.Dispatched
            || so.Status == Domain.Entities.SalesOrderStatus.PartiallyDispatched
            || so.Status == Domain.Entities.SalesOrderStatus.Confirmed)
        {
            var allComplete = so.Lines.All(l => l.DispatchedQuantity >= l.Quantity);
            var anyDispatched = so.Lines.Any(l => l.DispatchedQuantity > 0);
            if (allComplete)
                so.Status = Domain.Entities.SalesOrderStatus.Dispatched;
            else if (anyDispatched)
                so.Status = Domain.Entities.SalesOrderStatus.PartiallyDispatched;
            else
                so.Status = Domain.Entities.SalesOrderStatus.Confirmed;
        }

        crn.Status = Domain.Entities.CustomerReturnNoteStatus.Posted;
        crn.PostedAt = DateTimeOffset.UtcNow;
        crn.PostedBy = _currentUser.UserName;

        _repo.Update(crn);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerReturnNoteByIdQuery(crn.Id), cancellationToken);
    }
}
