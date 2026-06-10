using BengalTex.ERP.Application.Product.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Product.Commands;

public sealed record UpdateProductCommand(
    int Id,
    string Name,
    string? Specification,
    int ProductCategoryId,
    int UnitOfMeasureId,
    string? Size,
    string? Color,
    string? Material,
    string? HsCode,                 // BD HS code for export classification
    decimal SalesPrice,
    decimal ReorderLevel,
    bool IsStockItem,
    string? ImageUrl,
    string? Notes,
    bool IsActive
) : IRequest<ApiResponse<ProductDto>>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Specification).MaximumLength(1000);
        RuleFor(x => x.ProductCategoryId).GreaterThan(0);
        RuleFor(x => x.UnitOfMeasureId).GreaterThan(0);
        RuleFor(x => x.Size).MaximumLength(50);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.Material).MaximumLength(100);
        RuleFor(x => x.HsCode).MaximumLength(20);
        RuleFor(x => x.SalesPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateProductCommandHandler
    : IRequestHandler<UpdateProductCommand, ApiResponse<ProductDto>>
{
    private readonly IRepository<Domain.Entities.Product> _repo;
    private readonly IRepository<Domain.Entities.ProductCategory> _categoryRepo;
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _uomRepo;
    private readonly IUnitOfWork _uow;

    public UpdateProductCommandHandler(
        IRepository<Domain.Entities.Product> repo,
        IRepository<Domain.Entities.ProductCategory> categoryRepo,
        IRepository<Domain.Entities.UnitOfMeasure> uomRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _uomRepo = uomRepo;
        _uow = uow;
    }

    public async Task<ApiResponse<ProductDto>> Handle(
        UpdateProductCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<ProductDto>.Fail("Product not found.");

        var category = await _categoryRepo.GetByIdAsync(cmd.ProductCategoryId, cancellationToken);
        if (category is null)
            return ApiResponse<ProductDto>.Fail("Product category not found.");

        var uom = await _uomRepo.GetByIdAsync(cmd.UnitOfMeasureId, cancellationToken);
        if (uom is null)
            return ApiResponse<ProductDto>.Fail("Unit of measure not found.");

        // Code is identity — not editable.
        entity.Name = cmd.Name;
        entity.Specification = cmd.Specification;
        entity.ProductCategoryId = cmd.ProductCategoryId;
        entity.UnitOfMeasureId = cmd.UnitOfMeasureId;
        entity.Size = cmd.Size;
        entity.Color = cmd.Color;
        entity.Material = cmd.Material;
        entity.HsCode = string.IsNullOrWhiteSpace(cmd.HsCode) ? null : cmd.HsCode.Trim();
        entity.SalesPrice = cmd.SalesPrice;
        entity.ReorderLevel = cmd.ReorderLevel;
        entity.IsStockItem = cmd.IsStockItem;
        entity.ImageUrl = cmd.ImageUrl;
        entity.Notes = cmd.Notes;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new ProductDto(
            entity.Id, entity.Code, entity.Name, entity.Specification,
            entity.ProductCategoryId, category.Name,
            entity.UnitOfMeasureId, uom.Code,
            entity.Size, entity.Color, entity.Material, entity.HsCode,
            entity.SalesPrice, entity.ReorderLevel, entity.WeightedAverageCost, entity.IsStockItem,
            entity.ImageUrl, entity.Notes, entity.IsActive);

        return ApiResponse<ProductDto>.Ok(dto, "Product updated.");
    }
}
