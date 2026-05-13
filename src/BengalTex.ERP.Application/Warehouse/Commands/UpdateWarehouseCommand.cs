using BengalTex.ERP.Application.Warehouse.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Warehouse.Commands;

public sealed record UpdateWarehouseCommand(
    int Id,
    string Name,
    string WarehouseType,
    string? Address,
    bool IsActive
) : IRequest<ApiResponse<WarehouseDto>>;

public sealed class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseType).NotEmpty()
            .Must(t => Enum.TryParse<WarehouseType>(t, out _))
            .WithMessage("Type must be General, RawMaterial, FinishedGoods, WorkInProgress, or Reject.");
        RuleFor(x => x.Address).MaximumLength(300);
    }
}

internal sealed class UpdateWarehouseCommandHandler
    : IRequestHandler<UpdateWarehouseCommand, ApiResponse<WarehouseDto>>
{
    private readonly IRepository<Domain.Entities.Warehouse> _repo;
    private readonly IUnitOfWork _uow;

    public UpdateWarehouseCommandHandler(IRepository<Domain.Entities.Warehouse> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<WarehouseDto>> Handle(
        UpdateWarehouseCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.Query()
            .Include(w => w.Factory)
            .FirstOrDefaultAsync(w => w.Id == cmd.Id, cancellationToken);

        if (entity is null) return ApiResponse<WarehouseDto>.Fail("Warehouse not found.");

        // Code and FactoryId are intentionally not editable — they're identity for stock records.
        entity.Name = cmd.Name;
        entity.WarehouseType = Enum.Parse<WarehouseType>(cmd.WarehouseType);
        entity.Address = cmd.Address;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new WarehouseDto(
            entity.Id, entity.Code, entity.Name,
            entity.WarehouseType.ToString(), entity.Address,
            entity.FactoryId, entity.Factory?.Name, entity.IsActive);

        return ApiResponse<WarehouseDto>.Ok(dto, "Warehouse updated.");
    }
}
