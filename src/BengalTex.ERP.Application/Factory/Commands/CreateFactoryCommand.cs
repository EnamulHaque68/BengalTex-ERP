using BengalTex.ERP.Application.Factory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Factory.Commands;

public sealed record CreateFactoryCommand(
    string Name,
    string Code,
    int CompanyId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string? Phone,
    double? GeoFenceLat,
    double? GeoFenceLng,
    double? GeoFenceRadiusMeters
) : IRequest<ApiResponse<FactoryDto>>;

public sealed class CreateFactoryCommandValidator : AbstractValidator<CreateFactoryCommand>
{
    public CreateFactoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase alphanumeric (A-Z, 0-9, -, _).");
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.GeoFenceLat).InclusiveBetween(-90, 90).When(x => x.GeoFenceLat.HasValue);
        RuleFor(x => x.GeoFenceLng).InclusiveBetween(-180, 180).When(x => x.GeoFenceLng.HasValue);
        RuleFor(x => x.GeoFenceRadiusMeters).GreaterThan(0).When(x => x.GeoFenceRadiusMeters.HasValue);
    }
}

internal sealed class CreateFactoryCommandHandler : IRequestHandler<CreateFactoryCommand, ApiResponse<FactoryDto>>
{
    private readonly IRepository<Domain.Entities.Factory> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CreateFactoryCommandHandler(
        IRepository<Domain.Entities.Factory> repo,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<FactoryDto>> Handle(CreateFactoryCommand cmd, CancellationToken cancellationToken)
    {
        if (await _repo.AnyAsync(f => f.Code == cmd.Code, cancellationToken))
            return ApiResponse<FactoryDto>.Fail($"Factory code '{cmd.Code}' already exists.");

        // Hybrid pattern: explicit construction for Command → New Entity
        // (applies business rules like Code.ToUpperInvariant(), sets defaults)
        var factory = new Domain.Entities.Factory
        {
            Name = cmd.Name,
            Code = cmd.Code.ToUpperInvariant(),
            CompanyId = cmd.CompanyId,
            AddressLine1 = cmd.AddressLine1,
            AddressLine2 = cmd.AddressLine2,
            City = cmd.City,
            District = cmd.District,
            PostalCode = cmd.PostalCode,
            Phone = cmd.Phone,
            GeoFenceLat = cmd.GeoFenceLat,
            GeoFenceLng = cmd.GeoFenceLng,
            GeoFenceRadiusMeters = cmd.GeoFenceRadiusMeters,
            IsActive = true
        };

        await _repo.AddAsync(factory, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<FactoryDto>.Ok(_mapper.Map<FactoryDto>(factory), "Factory created.");
    }
}
