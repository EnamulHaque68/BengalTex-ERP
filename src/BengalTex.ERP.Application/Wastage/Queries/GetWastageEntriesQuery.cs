using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Wastage.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Wastage.Queries;

public sealed record GetWastageEntriesQuery(
    PagedQueryParameters Parameters,
    int? RawMaterialId = null,
    int? WastageReasonId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<WastageEntryListItemDto>>>;

internal sealed class GetWastageEntriesQueryHandler
    : IRequestHandler<GetWastageEntriesQuery, ApiResponse<PagedResult<WastageEntryListItemDto>>>
{
    private readonly IRepository<WastageEntry, long> _repo;
    public GetWastageEntriesQueryHandler(IRepository<WastageEntry, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<WastageEntryListItemDto>>> Handle(GetWastageEntriesQuery request, CancellationToken ct)
    {
        var query = _repo.Query();
        if (request.RawMaterialId.HasValue) query = query.Where(w => w.RawMaterialId == request.RawMaterialId.Value);
        if (request.WastageReasonId.HasValue) query = query.Where(w => w.WastageReasonId == request.WastageReasonId.Value);
        if (request.FromDate.HasValue) query = query.Where(w => w.WastageDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(w => w.WastageDate <= request.ToDate.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(w => w.Code.Contains(search) || w.RawMaterial.Name.Contains(search) ||
                (w.Department != null && w.Department.Contains(search)));

        query = query.OrderByDescending(w => w.WastageDate).ThenByDescending(w => w.Id);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(w => new WastageEntryListItemDto(
                w.Id, w.Code, w.WastageDate, w.RawMaterial.Name, w.WastageReason.Name, w.WastageReason.IsReusable,
                w.Quantity, w.TotalCost, w.Department))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<WastageEntryListItemDto>>.Ok(
            PagedResult<WastageEntryListItemDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, total));
    }
}

public sealed record GetWastageEntryByIdQuery(long Id) : IRequest<ApiResponse<WastageEntryDto>>;

internal sealed class GetWastageEntryByIdQueryHandler : IRequestHandler<GetWastageEntryByIdQuery, ApiResponse<WastageEntryDto>>
{
    private readonly IRepository<WastageEntry, long> _repo;
    public GetWastageEntryByIdQueryHandler(IRepository<WastageEntry, long> repo) => _repo = repo;

    public async Task<ApiResponse<WastageEntryDto>> Handle(GetWastageEntryByIdQuery request, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(w => w.Id == request.Id)
            .Select(w => new WastageEntryDto(
                w.Id, w.Code, w.WastageDate,
                w.ProductionOrderId, w.ProductionOrder != null ? w.ProductionOrder.Code : null,
                w.RawMaterialId, w.RawMaterial.Code, w.RawMaterial.Name, w.RawMaterial.UnitOfMeasure.Code,
                w.WastageReasonId, w.WastageReason.Name, w.WastageReason.IsReusable,
                w.Quantity, w.UnitCost, w.TotalCost, w.Department, w.Notes))
            .FirstOrDefaultAsync(ct);
        return dto is null ? ApiResponse<WastageEntryDto>.Fail("Wastage entry not found.") : ApiResponse<WastageEntryDto>.Ok(dto);
    }
}

/// <summary>Approved-wastage cost totals by reason over a period (variance/trend report).</summary>
public sealed record GetWastageSummaryQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<ApiResponse<WastageSummaryDto>>;

internal sealed class GetWastageSummaryQueryHandler : IRequestHandler<GetWastageSummaryQuery, ApiResponse<WastageSummaryDto>>
{
    private readonly IRepository<WastageEntry, long> _repo;
    public GetWastageSummaryQueryHandler(IRepository<WastageEntry, long> repo) => _repo = repo;

    public async Task<ApiResponse<WastageSummaryDto>> Handle(GetWastageSummaryQuery request, CancellationToken ct)
    {
        var rows = await _repo.Query()
            .Where(w => w.WastageDate >= request.FromDate && w.WastageDate <= request.ToDate)
            .GroupBy(w => new { w.WastageReasonId, w.WastageReason.Name, w.WastageReason.IsReusable })
            .Select(g => new WastageSummaryRowDto(g.Key.WastageReasonId, g.Key.Name, g.Key.IsReusable, g.Sum(x => x.TotalCost), g.Count()))
            .ToListAsync(ct);

        rows = rows.OrderByDescending(r => r.TotalCost).ToList();
        return ApiResponse<WastageSummaryDto>.Ok(new WastageSummaryDto(request.FromDate, request.ToDate, rows, rows.Sum(r => r.TotalCost)));
    }
}
