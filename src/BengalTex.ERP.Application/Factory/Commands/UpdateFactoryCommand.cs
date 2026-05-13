using BengalTex.ERP.Application.Factory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Factory.Commands;

public sealed record UpdateFactoryCommand(
    int Id,
    string Name,
    string Code,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string? Phone,
    bool IsActive,
    double? GeoFenceLat,
    double? GeoFenceLng,
    double? GeoFenceRadiusMeters
) : IRequest<ApiResponse<FactoryDto>>;

public sealed class UpdateFactoryCommandValidator : AbstractValidator<UpdateFactoryCommand>
{
    public UpdateFactoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase alphanumeric (A-Z, 0-9, -, _).");
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.GeoFenceLat).InclusiveBetween(-90, 90).When(x => x.GeoFenceLat.HasValue);
        RuleFor(x => x.GeoFenceLng).InclusiveBetween(-180, 180).When(x => x.GeoFenceLng.HasValue);
        RuleFor(x => x.GeoFenceRadiusMeters).GreaterThan(0).When(x => x.GeoFenceRadiusMeters.HasValue);
    }
}

internal sealed class UpdateFactoryCommandHandler : IRequestHandler<UpdateFactoryCommand, ApiResponse<FactoryDto>>
{
    private readonly IRepository<Domain.Entities.Factory> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateFactoryCommandHandler(
        IRepository<Domain.Entities.Factory> repo,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<FactoryDto>> Handle(UpdateFactoryCommand cmd, CancellationToken cancellationToken)
    {
        var factory = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (factory is null) return ApiResponse<FactoryDto>.Fail("Factory not found.");

        if (factory.Code != cmd.Code.ToUpperInvariant() &&
            await _repo.AnyAsync(f => f.Code == cmd.Code.ToUpperInvariant() && f.Id != cmd.Id, cancellationToken))
            return ApiResponse<FactoryDto>.Fail($"Factory code '{cmd.Code}' already exists.");

        // Hybrid pattern: explicit assignment for Command → Existing Entity
        // (protects audit fields, applies Code.ToUpperInvariant() business rule)
        factory.Name = cmd.Name;
        factory.Code = cmd.Code.ToUpperInvariant();
        factory.AddressLine1 = cmd.AddressLine1;
        factory.AddressLine2 = cmd.AddressLine2;
        factory.City = cmd.City;
        factory.District = cmd.District;
        factory.PostalCode = cmd.PostalCode;
        factory.Phone = cmd.Phone;
        factory.IsActive = cmd.IsActive;
        factory.GeoFenceLat = cmd.GeoFenceLat;
        factory.GeoFenceLng = cmd.GeoFenceLng;
        factory.GeoFenceRadiusMeters = cmd.GeoFenceRadiusMeters;

        _repo.Update(factory);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<FactoryDto>.Ok(_mapper.Map<FactoryDto>(factory), "Factory updated.");
    }
}
