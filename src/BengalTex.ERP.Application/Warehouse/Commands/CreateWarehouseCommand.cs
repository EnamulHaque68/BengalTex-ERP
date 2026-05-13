using BengalTex.ERP.Application.Warehouse.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Warehouse.Commands;

public sealed record CreateWarehouseCommand(
    string Code,
    string Name,
    string WarehouseType,        // "General" | "RawMaterial" | "FinishedGoods" | "WorkInProgress" | "Reject"
    string? Address,
    int FactoryId
) : IRequest<ApiResponse<WarehouseDto>>;

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase alphanumeric (A-Z, 0-9, -, _).");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseType).NotEmpty()
            .Must(t => Enum.TryParse<WarehouseType>(t, out _))
            .WithMessage("Type must be General, RawMaterial, FinishedGoods, WorkInProgress, or Reject.");
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.FactoryId).GreaterThan(0);
    }
}

internal sealed class CreateWarehouseCommandHandler
    : IRequestHandler<CreateWarehouseCommand, ApiResponse<WarehouseDto>>
{
    private readonly IRepository<Domain.Entities.Warehouse> _repo;
    private readonly IRepository<Domain.Entities.Factory> _factoryRepo;
    private readonly IUnitOfWork _uow;

    public CreateWarehouseCommandHandler(
        IRepository<Domain.Entities.Warehouse> repo,
        IRepository<Domain.Entities.Factory> factoryRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _factoryRepo = factoryRepo;
        _uow = uow;
    }

    public async Task<ApiResponse<WarehouseDto>> Handle(
        CreateWarehouseCommand cmd, CancellationToken cancellationToken)
    {
        var code = cmd.Code.ToUpperInvariant();

        // Factory must exist (FK check happens at SaveChanges, but a friendlier error here)
        var factory = await _factoryRepo.GetByIdAsync(cmd.FactoryId, cancellationToken);
        if (factory is null)
            return ApiResponse<WarehouseDto>.Fail("Factory not found.");

        // Uniqueness is per-factory (composite unique index on FactoryId+Code)
        if (await _repo.AnyAsync(w => w.FactoryId == cmd.FactoryId && w.Code == code, cancellationToken))
            return ApiResponse<WarehouseDto>.Fail(
                $"Warehouse code '{code}' already exists in factory '{factory.Code}'.");

        var entity = new Domain.Entities.Warehouse
        {
            Code = code,
            Name = cmd.Name,
            WarehouseType = Enum.Parse<WarehouseType>(cmd.WarehouseType),
            Address = cmd.Address,
            FactoryId = cmd.FactoryId,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseDto(
            entity.Id, entity.Code, entity.Name,
            entity.WarehouseType.ToString(), entity.Address,
            entity.FactoryId, factory.Name, entity.IsActive);

        return ApiResponse<WarehouseDto>.Ok(dto, "Warehouse created.");
    }
}
