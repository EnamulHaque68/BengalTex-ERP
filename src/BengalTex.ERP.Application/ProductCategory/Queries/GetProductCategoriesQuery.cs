using BengalTex.ERP.Application.ProductCategory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProductCategory.Queries;

public sealed record GetProductCategoriesQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<List<ProductCategoryDto>>>;

internal sealed class GetProductCategoriesQueryHandler
    : IRequestHandler<GetProductCategoriesQuery, ApiResponse<List<ProductCategoryDto>>>
{
    private readonly IRepository<Domain.Entities.ProductCategory> _repo;

    public GetProductCategoriesQueryHandler(IRepository<Domain.Entities.ProductCategory> repo) => _repo = repo;

    public async Task<ApiResponse<List<ProductCategoryDto>>> Handle(
        GetProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();
        if (!request.IncludeInactive)
            query = query.Where(c => c.IsActive);

        var list = await query
            .OrderBy(c => c.Name)
            .Select(c => new ProductCategoryDto(
                c.Id, c.Code, c.Name, c.Description,
                c.Products.Count(p => !p.IsDeleted),
                c.IsActive))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<ProductCategoryDto>>.Ok(list);
    }
}
