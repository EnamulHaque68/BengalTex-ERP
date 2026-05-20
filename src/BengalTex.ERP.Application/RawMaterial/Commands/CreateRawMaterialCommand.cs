using BengalTex.ERP.Application.RawMaterial.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.RawMaterial.Commands;

public sealed record CreateRawMaterialCommand(
    string? Code,                  // null → auto-gen via NumberingService("RM")
    string Name,
    string? Specification,
    string Category,
    int UnitOfMeasureId,
    decimal MinimumStockLevel,
    decimal OpeningStock,
    decimal StandardCost,
    int? PreferredSupplierId,
    string? Notes
) : IRequest<ApiResponse<RawMaterialDto>>;

public sealed class CreateRawMaterialCommandValidator : AbstractValidator<CreateRawMaterialCommand>
{
    public CreateRawMaterialCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50)
            .Matches("^[A-Z0-9/_-]+$")
                .When(x => !string.IsNullOrEmpty(x.Code))
                .WithMessage("Code must contain uppercase letters, digits, slash, hyphen, underscore.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Specification).MaximumLength(1000);
        RuleFor(x => x.Category).NotEmpty()
            .Must(c => Enum.TryParse<MaterialCategory>(c, out _))
            .WithMessage("Invalid material category.");
        RuleFor(x => x.UnitOfMeasureId).GreaterThan(0);
        RuleFor(x => x.MinimumStockLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OpeningStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StandardCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateRawMaterialCommandHandler
    : IRequestHandler<CreateRawMaterialCommand, ApiResponse<RawMaterialDto>>
{
    private readonly IRepository<Domain.Entities.RawMaterial> _repo;
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _uomRepo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateRawMaterialCommandHandler(
        IRepository<Domain.Entities.RawMaterial> repo,
        IRepository<Domain.Entities.UnitOfMeasure> uomRepo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo;
        _uomRepo = uomRepo;
        _supplierRepo = supplierRepo;
        _uow = uow;
        _numbering = numbering;
    }

    public async Task<ApiResponse<RawMaterialDto>> Handle(
        CreateRawMaterialCommand cmd, CancellationToken cancellationToken)
    {
        var uom = await _uomRepo.GetByIdAsync(cmd.UnitOfMeasureId, cancellationToken);
        if (uom is null)
            return ApiResponse<RawMaterialDto>.Fail("Unit of measure not found.");

        Domain.Entities.Supplier? supplier = null;
        if (cmd.PreferredSupplierId.HasValue)
        {
            supplier = await _supplierRepo.GetByIdAsync(cmd.PreferredSupplierId.Value, cancellationToken);
            if (supplier is null)
                return ApiResponse<RawMaterialDto>.Fail("Preferred supplier not found.");
        }

        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("RM", null, cancellationToken)
            : cmd.Code.Trim().ToUpperInvariant();

        if (await _repo.AnyAsync(r => r.Code == code, cancellationToken))
            return ApiResponse<RawMaterialDto>.Fail($"Raw material code '{code}' already exists.");

        var entity = new Domain.Entities.RawMaterial
        {
            Code = code,
            Name = cmd.Name,
            Specification = cmd.Specification,
            Category = Enum.Parse<MaterialCategory>(cmd.Category),
            UnitOfMeasureId = cmd.UnitOfMeasureId,
            MinimumStockLevel = cmd.MinimumStockLevel,
            OpeningStock = cmd.OpeningStock,
            StandardCost = cmd.StandardCost,
            PreferredSupplierId = cmd.PreferredSupplierId,
            Notes = cmd.Notes,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new RawMaterialDto(
            entity.Id, entity.Code, entity.Name, entity.Specification,
            entity.Category.ToString(),
            entity.UnitOfMeasureId, uom.Code,
            entity.MinimumStockLevel, entity.OpeningStock, entity.StandardCost, entity.WeightedAverageCost,
            entity.PreferredSupplierId, supplier?.Name,
            entity.Notes, entity.IsActive);

        return ApiResponse<RawMaterialDto>.Ok(dto, "Raw material created.");
    }
}
