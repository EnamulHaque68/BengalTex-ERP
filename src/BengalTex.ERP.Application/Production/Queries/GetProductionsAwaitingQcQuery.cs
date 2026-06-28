using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Queries;

/// <summary>One completed, QC-held production still awaiting inspection (remaining hold &gt; 0).</summary>
public record ProductionAwaitingQcDto(
    long Id,
    string Code,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal TotalQuantity,
    decimal RemainingQcQuantity);

/// <summary>
/// Lookup for the QC-inspection screen (Phase 5b): completed productions that are QC-held with a
/// remaining hold &gt; 0 — i.e. still have quantity to inspect. Productions fully cleared (remaining 0)
/// or never put on hold are excluded, so the dropdown only offers genuinely-pending work.
/// </summary>
public sealed record GetProductionsAwaitingQcQuery
    : IRequest<ApiResponse<IReadOnlyList<ProductionAwaitingQcDto>>>;

internal sealed class GetProductionsAwaitingQcQueryHandler
    : IRequestHandler<GetProductionsAwaitingQcQuery, ApiResponse<IReadOnlyList<ProductionAwaitingQcDto>>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IRepository<Domain.Entities.StockReservation, long> _reservationRepo;

    public GetProductionsAwaitingQcQueryHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.StockReservation, long> reservationRepo)
    {
        _repo = repo;
        _reservationRepo = reservationRepo;
    }

    public async Task<ApiResponse<IReadOnlyList<ProductionAwaitingQcDto>>> Handle(
        GetProductionsAwaitingQcQuery request, CancellationToken ct)
    {
        // Remaining hold per production = Σ Active "QcHold" reservation quantity (materialize-then-group).
        var heldRows = await _reservationRepo.Query()
            .AsNoTracking()
            .Where(r => r.ReferenceType == "QcHold" && r.Status == Domain.Entities.ReservationStatus.Active)
            .Select(r => new { r.ReferenceId, r.Quantity })
            .ToListAsync(ct);

        var remainingByPo = heldRows
            .GroupBy(x => x.ReferenceId)
            .Select(g => new { PoId = g.Key, Remaining = g.Sum(x => x.Quantity) })
            .Where(x => x.Remaining > 0m)
            .ToDictionary(x => x.PoId, x => x.Remaining);

        if (remainingByPo.Count == 0)
            return ApiResponse<IReadOnlyList<ProductionAwaitingQcDto>>.Ok(Array.Empty<ProductionAwaitingQcDto>());

        var poIds = remainingByPo.Keys.ToList();
        var prods = await _repo.Query()
            .AsNoTracking()
            .Where(p => poIds.Contains(p.Id) && p.Status == Domain.Entities.ProductionOrderStatus.Completed)
            .Select(p => new
            {
                p.Id,
                ProdCode = p.Code,
                p.ProductId,
                ProductCode = p.Product.Code,
                ProductName = p.Product.Name,
                Uom = p.Product.UnitOfMeasure.Code,
                p.Quantity
            })
            .ToListAsync(ct);

        var items = prods
            .Select(p => new ProductionAwaitingQcDto(
                p.Id, p.ProdCode, p.ProductId, p.ProductCode, p.ProductName, p.Uom,
                p.Quantity,
                remainingByPo.TryGetValue(p.Id, out var rem) ? rem : 0m))
            .OrderBy(p => p.Code)
            .ToList();

        return ApiResponse<IReadOnlyList<ProductionAwaitingQcDto>>.Ok(items);
    }
}
