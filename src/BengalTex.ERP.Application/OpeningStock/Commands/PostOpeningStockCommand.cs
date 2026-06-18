using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.OpeningStock.Dtos;
using BengalTex.ERP.Application.OpeningStock.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.OpeningStock.Commands;

/// <summary>
/// Posts a draft opening-stock entry. Atomic in one SaveChanges: for each line it seeds the
/// item's weighted-average cost from the entered unit cost, writes an OpeningStock movement
/// (inbound), and accumulates the inventory value; then posts ONE balanced journal —
/// Dr Raw Material Inventory + Dr Finished Goods Inventory / Cr Opening Balance Equity.
/// </summary>
public sealed record PostOpeningStockCommand(long Id) : IRequest<ApiResponse<OpeningStockEntryDto>>;

internal sealed class PostOpeningStockCommandHandler
    : IRequestHandler<PostOpeningStockCommand, ApiResponse<OpeningStockEntryDto>>
{
    private readonly IRepository<OpeningStockEntry, long> _repo;
    private readonly IStockService _stock;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public PostOpeningStockCommandHandler(
        IRepository<OpeningStockEntry, long> repo, IStockService stock, IJournalPostingService journal,
        ICurrentUserService currentUser, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo; _stock = stock; _journal = journal;
        _currentUser = currentUser; _uow = uow; _mediator = mediator;
    }

    public async Task<ApiResponse<OpeningStockEntryDto>> Handle(PostOpeningStockCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query()
            .Include(x => x.Lines).ThenInclude(l => l.RawMaterial)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);

        if (e is null) return ApiResponse<OpeningStockEntryDto>.Fail("Opening stock entry not found.");
        if (e.Status != OpeningStockStatus.Draft)
            return ApiResponse<OpeningStockEntryDto>.Fail("Only draft entries can be posted.");
        if (e.Lines.Count == 0)
            return ApiResponse<OpeningStockEntryDto>.Fail("Cannot post an entry with no lines.");

        var rmValue = 0m;
        var fgValue = 0m;

        foreach (var line in e.Lines)
        {
            var lineValue = line.Quantity * line.UnitCost;

            if (line.RawMaterialId is int rmId)
            {
                // Seed WAC (weighted-average into any existing stock — qtyBefore is ~0 at go-live).
                var qtyBefore = await _stock.GetRawMaterialTotalOnHandAsync(rmId, ct);
                var denom = qtyBefore + line.Quantity;
                if (denom > 0m)
                    line.RawMaterial!.WeightedAverageCost =
                        (qtyBefore * line.RawMaterial.WeightedAverageCost + line.Quantity * line.UnitCost) / denom;

                await _stock.PostRawMaterialMovementAsync(
                    rmId, e.WarehouseId, line.Quantity, StockMovementType.OpeningStock,
                    "OpeningStock", e.Id, e.Code, e.EntryDate, line.LineNotes, ct);
                rmValue += lineValue;
            }
            else if (line.ProductId is int pId)
            {
                var qtyBefore = await _stock.GetProductTotalOnHandAsync(pId, ct);
                var denom = qtyBefore + line.Quantity;
                if (denom > 0m)
                    line.Product!.WeightedAverageCost =
                        (qtyBefore * line.Product.WeightedAverageCost + line.Quantity * line.UnitCost) / denom;

                await _stock.PostProductMovementAsync(
                    pId, e.WarehouseId, line.Quantity, StockMovementType.OpeningStock,
                    "OpeningStock", e.Id, e.Code, e.EntryDate, line.LineNotes, ct);
                fgValue += lineValue;
            }
        }

        // One balanced opening journal — inventory in, equity out.
        var total = rmValue + fgValue;
        if (total > 0m)
        {
            var lines = new List<JournalPostingLine>();
            if (rmValue > 0m) lines.Add(new(LedgerAccounts.RawMaterialInventory, rmValue, 0m));
            if (fgValue > 0m) lines.Add(new(LedgerAccounts.FinishedGoodsInventory, fgValue, 0m));
            lines.Add(new(LedgerAccounts.OpeningBalanceEquity, 0m, total));

            await _journal.PostAsync(
                e.EntryDate, $"Opening stock {e.Code}", "OpeningStock", e.Id, e.Code, lines, ct);
        }

        e.Status = OpeningStockStatus.Posted;
        e.PostedAt = DateTimeOffset.UtcNow;
        e.PostedBy = _currentUser.UserName;
        _repo.Update(e);

        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetOpeningStockEntryByIdQuery(e.Id), ct);
    }
}
