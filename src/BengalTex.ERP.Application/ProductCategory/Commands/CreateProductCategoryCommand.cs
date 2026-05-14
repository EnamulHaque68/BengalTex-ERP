using BengalTex.ERP.Application.ProductCategory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.ProductCategory.Commands;

public sealed record CreateProductCategoryCommand(
    string Code,
    string Name,
    string? Description
) : IRequest<ApiResponse<ProductCategoryDto>>;

public sealed class CreateProductCategoryCommandValidator : AbstractValidator<CreateProductCategoryCommand>
{
    public CreateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase alphanumeric (A-Z, 0-9, -, _).");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateProductCategoryCommandHandler
    : IRequestHandler<CreateProductCategoryCommand, ApiResponse<ProductCategoryDto>>
{
    private readonly IRepository<Domain.Entities.ProductCategory> _repo;
    private readonly IUnitOfWork _uow;

    public CreateProductCategoryCommandHandler(
        IRepository<Domain.Entities.ProductCategory> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<ProductCategoryDto>> Handle(
        CreateProductCategoryCommand cmd, CancellationToken cancellationToken)
    {
        var code = cmd.Code.ToUpperInvariant();
        if (await _repo.AnyAsync(c => c.Code == code, cancellationToken))
            return ApiResponse<ProductCategoryDto>.Fail($"Product category code '{code}' already exists.");

        var entity = new Domain.Entities.ProductCategory
        {
            Code = code,
            Name = cmd.Name,
            Description = cmd.Description,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new ProductCategoryDto(entity.Id, entity.Code, entity.Name, entity.Description, 0, entity.IsActive);
        return ApiResponse<ProductCategoryDto>.Ok(dto, "Product category created.");
    }
}
