using BengalTex.ERP.Application.Product.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Product.Queries;

public sealed record GetProductByIdQuery(int Id) : IRequest<ApiResponse<ProductDto>>;

internal sealed class GetProductByIdQueryHandler
    : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDto>>
{
    private readonly IRepository<Domain.Entities.Product> _repo;

    public GetProductByIdQueryHandler(IRepository<Domain.Entities.Product> repo) => _repo = repo;

    public async Task<ApiResponse<ProductDto>> Handle(
        GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _repo.Query()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(
                p.Id, p.Code, p.Name, p.Specification,
                p.ProductCategoryId, p.ProductCategory.Name,
                p.UnitOfMeasureId, p.UnitOfMeasure.Code,
                p.Size, p.Color, p.Material, p.HsCode,
                p.SalesPrice, p.ReorderLevel, p.WeightedAverageCost, p.IsStockItem,
                p.ImageUrl, p.Notes, p.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ApiResponse<ProductDto>.Fail("Product not found.")
            : ApiResponse<ProductDto>.Ok(dto);
    }
}
