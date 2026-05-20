using BengalTex.ERP.Application.RawMaterial.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.RawMaterial.Commands;

public sealed record UpdateRawMaterialCommand(
    int Id,
    string Name,
    string? Specification,
    string Category,
    int UnitOfMeasureId,
    decimal MinimumStockLevel,
    decimal OpeningStock,
    decimal StandardCost,
    int? PreferredSupplierId,
    string? Notes,
    bool IsActive
) : IRequest<ApiResponse<RawMaterialDto>>;

public sealed class UpdateRawMaterialCommandValidator : AbstractValidator<UpdateRawMaterialCommand>
{
    public UpdateRawMaterialCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateRawMaterialCommandHandler
    : IRequestHandler<UpdateRawMaterialCommand, ApiResponse<RawMaterialDto>>
{
    private readonly IRepository<Domain.Entities.RawMaterial> _repo;
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _uomRepo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IUnitOfWork _uow;

    public UpdateRawMaterialCommandHandler(
        IRepository<Domain.Entities.RawMaterial> repo,
        IRepository<Domain.Entities.UnitOfMeasure> uomRepo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uomRepo = uomRepo;
        _supplierRepo = supplierRepo;
        _uow = uow;
    }

    public async Task<ApiResponse<RawMaterialDto>> Handle(
        UpdateRawMaterialCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<RawMaterialDto>.Fail("Raw material not found.");

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

        // Code is identity — not editable.
        entity.Name = cmd.Name;
        entity.Specification = cmd.Specification;
        entity.Category = Enum.Parse<MaterialCategory>(cmd.Category);
        entity.UnitOfMeasureId = cmd.UnitOfMeasureId;
        entity.MinimumStockLevel = cmd.MinimumStockLevel;
        entity.OpeningStock = cmd.OpeningStock;
        entity.StandardCost = cmd.StandardCost;
        entity.PreferredSupplierId = cmd.PreferredSupplierId;
        entity.Notes = cmd.Notes;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new RawMaterialDto(
            entity.Id, entity.Code, entity.Name, entity.Specification,
            entity.Category.ToString(),
            entity.UnitOfMeasureId, uom.Code,
            entity.MinimumStockLevel, entity.OpeningStock, entity.StandardCost, entity.WeightedAverageCost,
            entity.PreferredSupplierId, supplier?.Name,
            entity.Notes, entity.IsActive);

        return ApiResponse<RawMaterialDto>.Ok(dto, "Raw material updated.");
    }
}
