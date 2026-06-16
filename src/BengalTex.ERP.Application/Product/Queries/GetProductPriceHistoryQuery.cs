using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Product.Queries;

/// <summary>One recorded sales-price change for a product (newest first in the result).</summary>
public sealed record ProductPriceHistoryDto(
    int Id,
    decimal OldPrice,
    decimal NewPrice,
    decimal Change,              // NewPrice − OldPrice
    DateTimeOffset ChangedAt,
    string? ChangedBy);

public sealed record GetProductPriceHistoryQuery(int ProductId)
    : IRequest<ApiResponse<IReadOnlyList<ProductPriceHistoryDto>>>;

internal sealed class GetProductPriceHistoryQueryHandler
    : IRequestHandler<GetProductPriceHistoryQuery, ApiResponse<IReadOnlyList<ProductPriceHistoryDto>>>
{
    private readonly IRepository<Domain.Entities.ProductPriceHistory> _repo;
    public GetProductPriceHistoryQueryHandler(IRepository<Domain.Entities.ProductPriceHistory> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<ProductPriceHistoryDto>>> Handle(
        GetProductPriceHistoryQuery req, CancellationToken ct)
    {
        var items = await _repo.Query()
            .Where(h => h.ProductId == req.ProductId)
            .OrderByDescending(h => h.Id)
            .Select(h => new ProductPriceHistoryDto(
                h.Id, h.OldPrice, h.NewPrice, h.NewPrice - h.OldPrice, h.CreatedAt, h.CreatedBy))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<ProductPriceHistoryDto>>.Ok(items);
    }
}
