using BengalTex.ERP.Application.Bom.Dtos;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Bom.Queries;

public sealed record GetBomsQuery(
    PagedQueryParameters Parameters,
    int? ProductId = null,
    string? Status = null,
    bool ActiveOnly = false
) : IRequest<ApiResponse<PagedResult<BomListItemDto>>>;

internal sealed class GetBomsQueryHandler
    : IRequestHandler<GetBomsQuery, ApiResponse<PagedResult<BomListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;

    public GetBomsQueryHandler(IRepository<Domain.Entities.Bom> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<BomListItemDto>>> Handle(
        GetBomsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.ProductId.HasValue)
            query = query.Where(b => b.ProductId == request.ProductId.Value);

        if (request.ActiveOnly)
            query = query.Where(b => b.IsActive);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.BomStatus>(request.Status, out var status))
        {
            query = query.Where(b => b.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b =>
                b.Code.Contains(search) ||
                b.Product.Code.Contains(search) ||
                b.Product.Name.Contains(search) ||
                (b.Name != null && b.Name.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")    => query.OrderByDescending(b => b.Code),
            ("code", _)         => query.OrderBy(b => b.Code),
            ("product", "desc") => query.OrderByDescending(b => b.Product.Name),
            ("product", _)      => query.OrderBy(b => b.Product.Name),
            ("version", "desc") => query.OrderByDescending(b => b.Version),
            ("version", _)      => query.OrderBy(b => b.Version),
            _                   => query.OrderByDescending(b => b.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(b => new BomListItemDto(
                b.Id, b.Code, b.ProductId, b.Product.Name, b.Version,
                b.Status.ToString(), b.IsActive, b.OutputQuantity,
                b.Lines.Count,
                b.Lines.Sum(l => (decimal?)(l.Quantity * (1 + l.WastagePercent / 100m) * l.RawMaterial.StandardCost)) ?? 0m))
            .ToListAsync(cancellationToken);

        var result = PagedResult<BomListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<BomListItemDto>>.Ok(result);
    }
}
