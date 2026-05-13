using BengalTex.ERP.Application.Company.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Company.Commands;

public sealed record UpdateCompanyCommand(
    string Name,
    string ShortName,
    string? RegistrationNumber,
    string? TaxNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    string? PostalCode,
    string Country,
    string? Phone,
    string? Email,
    string? Website,
    string? LogoUrl
) : IRequest<ApiResponse<CompanyDto>>;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.RegistrationNumber).MaximumLength(100);
        RuleFor(x => x.TaxNumber).MaximumLength(50);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Website).MaximumLength(200);
    }
}

internal sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, ApiResponse<CompanyDto>>
{
    private readonly IRepository<Domain.Entities.Company> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateCompanyCommandHandler(
        IRepository<Domain.Entities.Company> repo,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CompanyDto>> Handle(UpdateCompanyCommand cmd, CancellationToken cancellationToken)
    {
        var company = await _repo.Query().FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            // First-time setup — create the singleton company record
            company = new Domain.Entities.Company();
            await _repo.AddAsync(company, cancellationToken);
        }

        // Hybrid pattern: explicit assignment for Command → Entity
        // (protects audit fields, IsDeleted, RowVersion, navigation collections)
        company.Name = cmd.Name;
        company.ShortName = cmd.ShortName;
        company.RegistrationNumber = cmd.RegistrationNumber;
        company.TaxNumber = cmd.TaxNumber;
        company.AddressLine1 = cmd.AddressLine1;
        company.AddressLine2 = cmd.AddressLine2;
        company.City = cmd.City;
        company.District = cmd.District;
        company.PostalCode = cmd.PostalCode;
        company.Country = cmd.Country;
        company.Phone = cmd.Phone;
        company.Email = cmd.Email;
        company.Website = cmd.Website;
        company.LogoUrl = cmd.LogoUrl;

        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<CompanyDto>.Ok(_mapper.Map<CompanyDto>(company), "Company profile updated.");
    }
}
