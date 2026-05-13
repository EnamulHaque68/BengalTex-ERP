using BengalTex.ERP.Application.UnitOfMeasure.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.UnitOfMeasure.Commands;

public sealed record UpdateUnitOfMeasureCommand(
    int Id,
    string Name,
    string Symbol,
    decimal ConversionFactor,
    bool IsActive
) : IRequest<ApiResponse<UnitOfMeasureDto>>;

public sealed class UpdateUnitOfMeasureCommandValidator : AbstractValidator<UpdateUnitOfMeasureCommand>
{
    public UpdateUnitOfMeasureCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ConversionFactor).GreaterThan(0);
    }
}

internal sealed class UpdateUnitOfMeasureCommandHandler
    : IRequestHandler<UpdateUnitOfMeasureCommand, ApiResponse<UnitOfMeasureDto>>
{
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _repo;
    private readonly IUnitOfWork _uow;

    public UpdateUnitOfMeasureCommandHandler(IRepository<Domain.Entities.UnitOfMeasure> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> Handle(
        UpdateUnitOfMeasureCommand cmd, CancellationToken cancellationToken)
    {
        // Bring base unit code along so the response stays consistent with GetById
        var entity = await _repo.Query()
            .Include(u => u.BaseUnit)
            .FirstOrDefaultAsync(u => u.Id == cmd.Id, cancellationToken);

        if (entity is null) return ApiResponse<UnitOfMeasureDto>.Fail("Unit of measure not found.");

        // Code, UnitType, and BaseUnit are intentionally not editable here — changing them
        // would invalidate any downstream stock / production records keyed by this unit.
        entity.Name = cmd.Name;
        entity.Symbol = cmd.Symbol;
        entity.ConversionFactor = entity.BaseUnitId is null ? 1m : cmd.ConversionFactor;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new UnitOfMeasureDto(
            entity.Id, entity.Code, entity.Name, entity.Symbol,
            entity.UnitType.ToString(), entity.BaseUnitId,
            entity.BaseUnit?.Code, entity.ConversionFactor, entity.IsActive);

        return ApiResponse<UnitOfMeasureDto>.Ok(dto, "Unit of measure updated.");
    }
}
