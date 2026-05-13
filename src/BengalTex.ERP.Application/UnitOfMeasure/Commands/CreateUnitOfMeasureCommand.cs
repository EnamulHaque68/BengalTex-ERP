using BengalTex.ERP.Application.UnitOfMeasure.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.UnitOfMeasure.Commands;

public sealed record CreateUnitOfMeasureCommand(
    string Code,
    string Name,
    string Symbol,
    string UnitType,           // "Count" | "Weight" | "Length" | "Volume" | "Area"
    int? BaseUnitId,
    decimal ConversionFactor
) : IRequest<ApiResponse<UnitOfMeasureDto>>;

public sealed class CreateUnitOfMeasureCommandValidator : AbstractValidator<CreateUnitOfMeasureCommand>
{
    public CreateUnitOfMeasureCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10)
            .Matches("^[A-Z0-9]+$").WithMessage("Code must be uppercase letters or digits only.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.UnitType).NotEmpty()
            .Must(t => Enum.TryParse<UnitType>(t, out _))
            .WithMessage("UnitType must be Count, Weight, Length, Volume, or Area.");
        RuleFor(x => x.ConversionFactor).GreaterThan(0);
    }
}

internal sealed class CreateUnitOfMeasureCommandHandler
    : IRequestHandler<CreateUnitOfMeasureCommand, ApiResponse<UnitOfMeasureDto>>
{
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _repo;
    private readonly IUnitOfWork _uow;

    public CreateUnitOfMeasureCommandHandler(IRepository<Domain.Entities.UnitOfMeasure> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<UnitOfMeasureDto>> Handle(
        CreateUnitOfMeasureCommand cmd, CancellationToken cancellationToken)
    {
        var code = cmd.Code.ToUpperInvariant();
        if (await _repo.AnyAsync(u => u.Code == code, cancellationToken))
            return ApiResponse<UnitOfMeasureDto>.Fail($"Unit code '{code}' already exists.");

        var unitType = Enum.Parse<UnitType>(cmd.UnitType);

        // If BaseUnitId provided, validate it exists AND is the same UnitType
        string? baseUnitCode = null;
        if (cmd.BaseUnitId.HasValue)
        {
            var baseUnit = await _repo.GetByIdAsync(cmd.BaseUnitId.Value, cancellationToken);
            if (baseUnit is null)
                return ApiResponse<UnitOfMeasureDto>.Fail("Base unit not found.");
            if (baseUnit.UnitType != unitType)
                return ApiResponse<UnitOfMeasureDto>.Fail(
                    $"Base unit '{baseUnit.Code}' is of type {baseUnit.UnitType}, but this unit is {unitType}. They must match.");
            if (baseUnit.BaseUnitId is not null)
                return ApiResponse<UnitOfMeasureDto>.Fail(
                    $"Base unit '{baseUnit.Code}' is itself a derivative. Pick a true base unit.");
            baseUnitCode = baseUnit.Code;
        }

        var entity = new Domain.Entities.UnitOfMeasure
        {
            Code = code,
            Name = cmd.Name,
            Symbol = cmd.Symbol,
            UnitType = unitType,
            BaseUnitId = cmd.BaseUnitId,
            ConversionFactor = cmd.BaseUnitId is null ? 1m : cmd.ConversionFactor,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = new UnitOfMeasureDto(
            entity.Id, entity.Code, entity.Name, entity.Symbol,
            entity.UnitType.ToString(), entity.BaseUnitId, baseUnitCode,
            entity.ConversionFactor, entity.IsActive);

        return ApiResponse<UnitOfMeasureDto>.Ok(dto, "Unit of measure created.");
    }
}
