using BengalTex.ERP.Application.Customer.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Customer.Commands;

/// <summary>
/// Update customer profile. Code is identity and intentionally NOT editable here —
/// any downstream sales orders / invoices reference the customer by Code in printouts.
/// </summary>
public sealed record UpdateCustomerCommand(
    int Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Website,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string Country,
    string? BinNumber,
    string? VatNumber,
    string? TinNumber,
    string Category,
    decimal CreditLimit,
    int CreditPeriodDays,
    bool IsExport,
    string? Notes,
    bool IsActive,
    int? ParentCustomerId = null
) : IRequest<ApiResponse<CustomerDto>>;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email)).MaximumLength(200);
        RuleFor(x => x.Website).MaximumLength(200);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AddressLine2).MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.District).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BinNumber).MaximumLength(50);
        RuleFor(x => x.VatNumber).MaximumLength(50);
        RuleFor(x => x.TinNumber).MaximumLength(50);
        RuleFor(x => x.Category).NotEmpty()
            .Must(c => Enum.TryParse<CustomerCategory>(c, out _))
            .WithMessage("Category must be A, B, or C.");
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CreditPeriodDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateCustomerCommandHandler
    : IRequestHandler<UpdateCustomerCommand, ApiResponse<CustomerDto>>
{
    private readonly IRepository<Domain.Entities.Customer> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateCustomerCommandHandler(
        IRepository<Domain.Entities.Customer> repo,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CustomerDto>> Handle(
        UpdateCustomerCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<CustomerDto>.Fail("Customer not found.");

        if (cmd.ParentCustomerId is int parentId)
        {
            if (parentId == cmd.Id) return ApiResponse<CustomerDto>.Fail("A customer cannot be its own parent.");
            if (!await _repo.AnyAsync(c => c.Id == parentId, cancellationToken))
                return ApiResponse<CustomerDto>.Fail("Parent customer not found.");
        }

        entity.Name = cmd.Name;
        entity.ContactPerson = cmd.ContactPerson;
        entity.Phone = cmd.Phone;
        entity.Email = cmd.Email;
        entity.Website = cmd.Website;
        entity.AddressLine1 = cmd.AddressLine1;
        entity.AddressLine2 = cmd.AddressLine2;
        entity.City = cmd.City;
        entity.District = cmd.District;
        entity.PostalCode = cmd.PostalCode;
        entity.Country = cmd.Country;
        entity.BinNumber = cmd.BinNumber;
        entity.VatNumber = cmd.VatNumber;
        entity.TinNumber = cmd.TinNumber;
        entity.Category = Enum.Parse<CustomerCategory>(cmd.Category);
        entity.CreditLimit = cmd.CreditLimit;
        entity.CreditPeriodDays = cmd.CreditPeriodDays;
        entity.IsExport = cmd.IsExport;
        entity.ParentCustomerId = cmd.ParentCustomerId;
        entity.Notes = cmd.Notes;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerDto>.Ok(_mapper.Map<CustomerDto>(entity), "Customer updated.");
    }
}
