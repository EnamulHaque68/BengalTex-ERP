using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProductVariants;

/// <summary>All variants of a single product, ordered by code.</summary>
public sealed record GetProductVariantsQuery(int ProductId) : IRequest<ApiResponse<IReadOnlyList<ProductVariantDto>>>;

internal sealed class GetProductVariantsQueryHandler
    : IRequestHandler<GetProductVariantsQuery, ApiResponse<IReadOnlyList<ProductVariantDto>>>
{
    private readonly IRepository<Domain.Entities.ProductVariant> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;

    public GetProductVariantsQueryHandler(
        IRepository<Domain.Entities.ProductVariant> repo,
        IRepository<Domain.Entities.Product> productRepo)
    {
        _repo = repo;
        _productRepo = productRepo;
    }

    public async Task<ApiResponse<IReadOnlyList<ProductVariantDto>>> Handle(
        GetProductVariantsQuery req, CancellationToken ct)
    {
        var product = await _productRepo.GetByIdAsync(req.ProductId, ct);
        if (product is null) return ApiResponse<IReadOnlyList<ProductVariantDto>>.Fail("Product not found.");

        var variants = await _repo.Query().AsNoTracking()
            .Where(v => v.ProductId == req.ProductId)
            .OrderBy(v => v.VariantCode)
            .ToListAsync(ct);

        var dtos = variants.Select(v => new ProductVariantDto(
            v.Id, v.ProductId, v.VariantCode, v.Name, v.Color, v.Size, v.Sku,
            v.SalesPriceOverride, v.SalesPriceOverride ?? product.SalesPrice,
            v.Notes, v.IsActive)).ToList();

        return ApiResponse<IReadOnlyList<ProductVariantDto>>.Ok(dtos);
    }
}
