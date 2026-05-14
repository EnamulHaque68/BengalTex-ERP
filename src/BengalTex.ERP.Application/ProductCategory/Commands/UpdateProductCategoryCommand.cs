using BengalTex.ERP.Application.ProductCategory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProductCategory.Commands;

public sealed record UpdateProductCategoryCommand(
    int Id,
    string Name,
    string? Description,
    bool IsActive
) : IRequest<ApiResponse<ProductCategoryDto>>;

public sealed class UpdateProductCategoryCommandValidator : AbstractValidator<UpdateProductCategoryCommand>
{
    public UpdateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateProductCategoryCommandHandler
    : IRequestHandler<UpdateProductCategoryCommand, ApiResponse<ProductCategoryDto>>
{
    private readonly IRepository<Domain.Entities.ProductCategory> _repo;
    private readonly IUnitOfWork _uow;

    public UpdateProductCategoryCommandHandler(
        IRepository<Domain.Entities.ProductCategory> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<ProductCategoryDto>> Handle(
        UpdateProductCategoryCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<ProductCategoryDto>.Fail("Product category not found.");

        // Code is identity — not editable here.
        entity.Name = cmd.Name;
        entity.Description = cmd.Description;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        var productCount = await _repo.Query()
            .Where(c => c.Id == cmd.Id)
            .Select(c => c.Products.Count(p => !p.IsDeleted))
            .FirstAsync(cancellationToken);

        var dto = new ProductCategoryDto(entity.Id, entity.Code, entity.Name, entity.Description, productCount, entity.IsActive);
        return ApiResponse<ProductCategoryDto>.Ok(dto, "Product category updated.");
    }
}
