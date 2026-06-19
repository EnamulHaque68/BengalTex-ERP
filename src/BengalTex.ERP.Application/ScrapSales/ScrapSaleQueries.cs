using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ScrapSales;

public sealed record GetScrapSalesQuery(PagedQueryParameters Parameters, string? Status = null)
    : IRequest<ApiResponse<PagedResult<ScrapSaleListItemDto>>>;

internal sealed class GetScrapSalesQueryHandler
    : IRequestHandler<GetScrapSalesQuery, ApiResponse<PagedResult<ScrapSaleListItemDto>>>
{
    private readonly IRepository<ScrapSale, long> _repo;
    public GetScrapSalesQueryHandler(IRepository<ScrapSale, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<ScrapSaleListItemDto>>> Handle(GetScrapSalesQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<ScrapSaleStatus>(req.Status, true, out var s))
            q = q.Where(x => x.Status == s);
        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search) || (x.BuyerName != null && x.BuyerName.Contains(search)));

        q = q.OrderByDescending(x => x.Id);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((req.Parameters.Page - 1) * req.Parameters.PageSize).Take(req.Parameters.PageSize)
            .Select(x => new ScrapSaleListItemDto(
                x.Id, x.Code, x.SaleDate, x.BuyerName, x.PaymentMethod.ToString(), x.Status.ToString(),
                x.Lines.Count, x.Lines.Sum(l => l.Quantity * l.UnitPrice)))
            .ToListAsync(ct);
        return ApiResponse<PagedResult<ScrapSaleListItemDto>>.Ok(
            PagedResult<ScrapSaleListItemDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

public sealed record GetScrapSaleByIdQuery(long Id) : IRequest<ApiResponse<ScrapSaleDto>>;

internal sealed class GetScrapSaleByIdQueryHandler
    : IRequestHandler<GetScrapSaleByIdQuery, ApiResponse<ScrapSaleDto>>
{
    private readonly IRepository<ScrapSale, long> _repo;
    public GetScrapSaleByIdQueryHandler(IRepository<ScrapSale, long> repo) => _repo = repo;

    public async Task<ApiResponse<ScrapSaleDto>> Handle(GetScrapSaleByIdQuery req, CancellationToken ct)
    {
        var s = await _repo.Query().AsNoTracking().Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (s is null) return ApiResponse<ScrapSaleDto>.Fail("Scrap sale not found.");

        var lines = s.Lines.OrderBy(l => l.SortOrder)
            .Select(l => new ScrapSaleLineDto(l.Id, l.Description, l.Quantity, l.Unit, l.UnitPrice, l.Quantity * l.UnitPrice, l.SortOrder))
            .ToList();
        return ApiResponse<ScrapSaleDto>.Ok(new ScrapSaleDto(
            s.Id, s.Code, s.SaleDate, s.BuyerName, s.PaymentMethod.ToString(), s.Status.ToString(),
            s.PostedAt, s.PostedBy, s.Notes, lines.Sum(l => l.LineTotal), lines));
    }
}
