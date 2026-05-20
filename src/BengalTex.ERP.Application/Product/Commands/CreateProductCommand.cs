using BengalTex.ERP.Application.Product.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Product.Commands;

public sealed record CreateProductCommand(
    string? Code,                  // null → auto-gen via NumberingService("PROD")
    string Name,
    string? Specification,
    int ProductCategoryId,
    int UnitOfMeasureId,
    string? Size,
    string? Color,
    string? Material,
    decimal SalesPrice,
    decimal ReorderLevel,
    bool IsStockItem,
    string? ImageUrl,
    string? Notes
) : IRequest<ApiResponse<ProductDto>>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50)
            .Matches("^[A-Z0-9/_-]+$")
                .When(x => !string.IsNullOrEmpty(x.Code))
                .WithMessage("Code must contain uppercase letters, digits, slash, hyphen, underscore.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Specification).MaximumLength(1000);
        RuleFor(x => x.ProductCategoryId).GreaterThan(0);
        RuleFor(x => x.UnitOfMeasureId).GreaterThan(0);
        RuleFor(x => x.Size).MaximumLength(50);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.Material).MaximumLength(100);
        RuleFor(x => x.SalesPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, ApiResponse<ProductDto>>
{
    private readonly IRepository<Domain.Entities.Product> _repo;
    private readonly IRepository<Domain.Entities.ProductCategory> _categoryRepo;
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _uomRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateProductCommandHandler(
        IRepository<Domain.Entities.Product> repo,
        IRepository<Domain.Entities.ProductCategory> categoryRepo,
        IRepository<Domain.Entities.UnitOfMeasure> uomRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _uomRepo = uomRepo;
        _uow = uow;
        _numbering = numbering;
    }

    public async Task<ApiResponse<ProductDto>> Handle(
        CreateProductCommand cmd, CancellationToken cancellationToken)
    {
        // FK pre-checks for friendly errors (FK violation would otherwise be a 500)
        var category = await _categoryRepo.GetByIdAsync(cmd.ProductCategoryId, cancellationToken);
        if (category is null)
            return ApiResponse<ProductDto>.Fail("Product category not found.");

        var uom = await _uomRepo.GetByIdAsync(cmd.UnitOfMeasureId, cancellationToken);
        if (uom is null)
            return ApiResponse<ProductDto>.Fail("Unit of measure not found.");

        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("PROD", null, cancellationToken)
            : cmd.Code.Trim().ToUpperInvariant();

        if (await _repo.AnyAsync(p => p.Code == code, cancellationToken))
            return ApiResponse<ProductDto>.Fail($"Product code '{code}' already exists.");

        var entity = new Domain.Entities.Product
        {
            Code = code,
            Name = cmd.Name,
            Specification = cmd.Specification,
            ProductCategoryId = cmd.ProductCategoryId,
            UnitOfMeasureId = cmd.UnitOfMeasureId,
            Size = cmd.Size,
            Color = cmd.Color,
            Material = cmd.Material,
            SalesPrice = cmd.SalesPrice,
            ReorderLevel = cmd.ReorderLevel,
            IsStockItem = cmd.IsStockItem,
            ImageUrl = cmd.ImageUrl,
            Notes = cmd.Notes,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new ProductDto(
            entity.Id, entity.Code, entity.Name, entity.Specification,
            entity.ProductCategoryId, category.Name,
            entity.UnitOfMeasureId, uom.Code,
            entity.Size, entity.Color, entity.Material,
            entity.SalesPrice, entity.ReorderLevel, entity.WeightedAverageCost, entity.IsStockItem,
            entity.ImageUrl, entity.Notes, entity.IsActive);

        return ApiResponse<ProductDto>.Ok(dto, "Product created.");
    }
}
