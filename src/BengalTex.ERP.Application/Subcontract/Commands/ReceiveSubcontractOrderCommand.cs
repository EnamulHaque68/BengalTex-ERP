using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.Subcontract.Dtos;
using BengalTex.ERP.Application.Subcontract.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Subcontract.Commands;

public sealed record SubcontractReceiveLineInput(long LineId, decimal ReceivedQuantity);

/// <summary>
/// Receives processed material back from the subcontractor into the order warehouse.
/// Per-line received quantity (≤ issued; shortfall = wastage). Two-pass atomic:
///   1. Validate each line belongs to the order and 0 ≤ received ≤ issued.
///   2. Post a SubcontractReceiveIn movement (+) per line with received > 0; flip to Received.
/// </summary>
public sealed record ReceiveSubcontractOrderCommand(
    long Id,
    IReadOnlyList<SubcontractReceiveLineInput> Lines
) : IRequest<ApiResponse<SubcontractOrderDto>>;

public sealed class ReceiveSubcontractOrderCommandValidator : AbstractValidator<ReceiveSubcontractOrderCommand>
{
    public ReceiveSubcontractOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.LineId).GreaterThan(0);
            line.RuleFor(l => l.ReceivedQuantity).GreaterThanOrEqualTo(0);
        });
    }
}

internal sealed class ReceiveSubcontractOrderCommandHandler
    : IRequestHandler<ReceiveSubcontractOrderCommand, ApiResponse<SubcontractOrderDto>>
{
    private readonly IRepository<SubcontractOrder, long> _repo;
    private readonly IStockService _stock;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ReceiveSubcontractOrderCommandHandler(
        IRepository<SubcontractOrder, long> repo, IStockService stock, IUnitOfWork uow,
        ICurrentUserService currentUser, IMediator mediator)
    {
        _repo = repo;
        _stock = stock;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SubcontractOrderDto>> Handle(
        ReceiveSubcontractOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);

        if (order is null) return ApiResponse<SubcontractOrderDto>.Fail("Subcontract order not found.");
        if (order.Status != SubcontractStatus.Issued)
            return ApiResponse<SubcontractOrderDto>.Fail("Only issued subcontract orders can be received.");

        // Pass 1 — validate
        foreach (var input in cmd.Lines)
        {
            var line = order.Lines.FirstOrDefault(l => l.Id == input.LineId);
            if (line is null)
                return ApiResponse<SubcontractOrderDto>.Fail($"Line {input.LineId} does not belong to this order.");
            if (input.ReceivedQuantity > line.IssuedQuantity)
                return ApiResponse<SubcontractOrderDto>.Fail(
                    $"Received quantity ({input.ReceivedQuantity:0.####}) cannot exceed issued ({line.IssuedQuantity:0.####}).");
        }

        // Pass 2 — set received + move in
        foreach (var input in cmd.Lines)
        {
            var line = order.Lines.First(l => l.Id == input.LineId);
            line.ReceivedQuantity = input.ReceivedQuantity;
            if (input.ReceivedQuantity <= 0) continue;

            if (line.RawMaterialId.HasValue)
                await _stock.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, order.WarehouseId, input.ReceivedQuantity,
                    StockMovementType.SubcontractReceiveIn, "SubcontractOrder", order.Id, order.Code,
                    DateOnly.FromDateTime(DateTime.UtcNow), line.LineNotes, ct);
            else
                await _stock.PostProductMovementAsync(
                    line.ProductId!.Value, order.WarehouseId, input.ReceivedQuantity,
                    StockMovementType.SubcontractReceiveIn, "SubcontractOrder", order.Id, order.Code,
                    DateOnly.FromDateTime(DateTime.UtcNow), line.LineNotes, ct);
        }

        order.Status = SubcontractStatus.Received;
        order.ReceivedAt = DateTimeOffset.UtcNow;
        order.ReceivedBy = _currentUser.UserName;
        _repo.Update(order);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetSubcontractOrderByIdQuery(order.Id), ct);
    }
}
