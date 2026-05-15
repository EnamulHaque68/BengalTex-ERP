using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Inventory.Dtos;
using BengalTex.ERP.Application.Inventory.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Inventory.Commands;

/// <summary>
/// Posts a draft stock adjustment. Atomic in one SaveChanges: each line is turned into
/// a <see cref="StockMovement"/> via <see cref="IStockService"/> (which also upserts the
/// <see cref="StockOnHand"/> snapshot), and the adjustment is marked Posted with
/// PostedAt/By. Once Posted, the adjustment is immutable.
/// </summary>
public sealed record PostStockAdjustmentCommand(long Id) : IRequest<ApiResponse<StockAdjustmentDto>>;

internal sealed class PostStockAdjustmentCommandHandler
    : IRequestHandler<PostStockAdjustmentCommand, ApiResponse<StockAdjustmentDto>>
{
    private readonly IRepository<Domain.Entities.StockAdjustment, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStockService _stock;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostStockAdjustmentCommandHandler(
        IRepository<Domain.Entities.StockAdjustment, long> repo,
        IUnitOfWork uow,
        IStockService stock,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _stock = stock;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StockAdjustmentDto>> Handle(
        PostStockAdjustmentCommand cmd, CancellationToken cancellationToken)
    {
        var adj = await _repo.Query()
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.Id, cancellationToken);

        if (adj is null) return ApiResponse<StockAdjustmentDto>.Fail("Stock adjustment not found.");
        if (adj.Status != Domain.Entities.StockAdjustmentStatus.Draft)
            return ApiResponse<StockAdjustmentDto>.Fail("Only draft stock adjustments can be posted.");
        if (adj.Lines.Count == 0)
            return ApiResponse<StockAdjustmentDto>.Fail("Cannot post a stock adjustment with no lines.");

        foreach (var line in adj.Lines)
        {
            var movementType = line.SignedQuantity > 0
                ? StockMovementType.AdjustmentIn
                : StockMovementType.AdjustmentOut;

            await _stock.PostMovementAsync(
                line.RawMaterialId,
                adj.WarehouseId,
                line.SignedQuantity,
                movementType,
                referenceType: "Adjustment",
                referenceId: adj.Id,
                referenceCode: adj.Code,
                movementDate: adj.AdjustmentDate,
                notes: line.LineNotes,
                ct: cancellationToken);
        }

        adj.Status = Domain.Entities.StockAdjustmentStatus.Posted;
        adj.PostedAt = DateTimeOffset.UtcNow;
        adj.PostedBy = _currentUser.UserName;

        _repo.Update(adj);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetStockAdjustmentByIdQuery(adj.Id), cancellationToken);
    }
}
