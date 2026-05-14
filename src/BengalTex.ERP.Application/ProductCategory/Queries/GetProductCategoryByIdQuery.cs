using BengalTex.ERP.Application.ProductCategory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProductCategory.Queries;

public sealed record GetProductCategoryByIdQuery(int Id) : IRequest<ApiResponse<ProductCategoryDto>>;

internal sealed class GetProductCategoryByIdQueryHandler
    : IRequestHandler<GetProductCategoryByIdQuery, ApiResponse<ProductCategoryDto>>
{
    private readonly IRepository<Domain.Entities.ProductCategory> _repo;

    public GetProductCategoryByIdQueryHandler(IRepository<Domain.Entities.ProductCategory> repo) => _repo = repo;

    public async Task<ApiResponse<ProductCategoryDto>> Handle(
        GetProductCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _repo.Query()
            .Where(c => c.Id == request.Id)
            .Select(c => new ProductCategoryDto(
                c.Id, c.Code, c.Name, c.Description,
                c.Products.Count(p => !p.IsDeleted),
                c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ApiResponse<ProductCategoryDto>.Fail("Product category not found.")
            : ApiResponse<ProductCategoryDto>.Ok(dto);
    }
}
