using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.OpeningStock.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.OpeningStock.Queries;

// ── List ──
public sealed record GetOpeningStockEntriesQuery(PagedQueryParameters Parameters, string? Status = null)
    : IRequest<ApiResponse<PagedResult<OpeningStockListItemDto>>>;

internal sealed class GetOpeningStockEntriesQueryHandler
    : IRequestHandler<GetOpeningStockEntriesQuery, ApiResponse<PagedResult<OpeningStockListItemDto>>>
{
    private readonly IRepository<OpeningStockEntry, long> _repo;
    public GetOpeningStockEntriesQueryHandler(IRepository<OpeningStockEntry, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<OpeningStockListItemDto>>> Handle(
        GetOpeningStockEntriesQuery req, CancellationToken ct)
    {
        var query = _repo.Query();
        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<OpeningStockStatus>(req.Status, true, out var st))
            query = query.Where(e => e.Status == st);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(e => e.Code.Contains(search));

        query = query.OrderByDescending(e => e.Id);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(e => new OpeningStockListItemDto(
                e.Id, e.Code, e.EntryDate, e.Warehouse.Code, e.Status.ToString(),
                e.Lines.Count, e.Lines.Sum(l => l.Quantity * l.UnitCost)))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<OpeningStockListItemDto>>.Ok(
            PagedResult<OpeningStockListItemDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ── By id ──
public sealed record GetOpeningStockEntryByIdQuery(long Id) : IRequest<ApiResponse<OpeningStockEntryDto>>;

internal sealed class GetOpeningStockEntryByIdQueryHandler
    : IRequestHandler<GetOpeningStockEntryByIdQuery, ApiResponse<OpeningStockEntryDto>>
{
    private readonly IRepository<OpeningStockEntry, long> _repo;
    public GetOpeningStockEntryByIdQueryHandler(IRepository<OpeningStockEntry, long> repo) => _repo = repo;

    public async Task<ApiResponse<OpeningStockEntryDto>> Handle(GetOpeningStockEntryByIdQuery req, CancellationToken ct)
    {
        var e = await _repo.Query().AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(r => r!.UnitOfMeasure)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (e is null) return ApiResponse<OpeningStockEntryDto>.Fail("Opening stock entry not found.");

        var lines = e.Lines.OrderBy(l => l.SortOrder).Select(l =>
        {
            var isRm = l.RawMaterialId != null;
            return new OpeningStockLineDto(
                l.Id,
                isRm ? "RawMaterial" : "Product",
                l.RawMaterialId, l.ProductId,
                isRm ? l.RawMaterial!.Code : l.Product!.Code,
                isRm ? l.RawMaterial!.Name : l.Product!.Name,
                isRm ? l.RawMaterial!.UnitOfMeasure.Code : l.Product!.UnitOfMeasure.Code,
                l.Quantity, l.UnitCost, l.Quantity * l.UnitCost,
                l.SortOrder, l.LineNotes);
        }).ToList();

        var dto = new OpeningStockEntryDto(
            e.Id, e.Code, e.EntryDate, e.WarehouseId, e.Warehouse.Name,
            e.Status.ToString(), e.PostedAt, e.PostedBy, e.Notes,
            lines.Sum(l => l.LineValue), lines);

        return ApiResponse<OpeningStockEntryDto>.Ok(dto);
    }
}
