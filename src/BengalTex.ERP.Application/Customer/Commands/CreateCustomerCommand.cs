using BengalTex.ERP.Application.Customer.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Customer.Commands;

public sealed record CreateCustomerCommand(
    string? Code,                  // Optional — auto-generated via NumberingService("CUST") if empty
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
    string Category,               // "A" | "B" | "C"
    decimal CreditLimit,
    int CreditPeriodDays,
    bool IsExport,
    string? Notes,
    int? ParentCustomerId = null
) : IRequest<ApiResponse<CustomerDto>>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        // Code is optional but when supplied must match the format
        RuleFor(x => x.Code).MaximumLength(50)
            .Matches("^[A-Z0-9/_-]+$")
                .When(x => !string.IsNullOrEmpty(x.Code))
                .WithMessage("Code must contain uppercase letters, digits, slash, hyphen, underscore.");

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

internal sealed class CreateCustomerCommandHandler
    : IRequestHandler<CreateCustomerCommand, ApiResponse<CustomerDto>>
{
    private readonly IRepository<Domain.Entities.Customer> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INumberingService _numbering;

    public CreateCustomerCommandHandler(
        IRepository<Domain.Entities.Customer> repo,
        IUnitOfWork uow,
        IMapper mapper,
        INumberingService numbering)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
        _numbering = numbering;
    }

    public async Task<ApiResponse<CustomerDto>> Handle(
        CreateCustomerCommand cmd, CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("CUST", null, cancellationToken)
            : cmd.Code.Trim().ToUpperInvariant();

        if (await _repo.AnyAsync(c => c.Code == code, cancellationToken))
            return ApiResponse<CustomerDto>.Fail($"Customer code '{code}' already exists.");

        if (cmd.ParentCustomerId is int parentId && !await _repo.AnyAsync(c => c.Id == parentId, cancellationToken))
            return ApiResponse<CustomerDto>.Fail("Parent customer not found.");

        var entity = new Domain.Entities.Customer
        {
            Code = code,
            Name = cmd.Name,
            ContactPerson = cmd.ContactPerson,
            Phone = cmd.Phone,
            Email = cmd.Email,
            Website = cmd.Website,
            AddressLine1 = cmd.AddressLine1,
            AddressLine2 = cmd.AddressLine2,
            City = cmd.City,
            District = cmd.District,
            PostalCode = cmd.PostalCode,
            Country = cmd.Country,
            BinNumber = cmd.BinNumber,
            VatNumber = cmd.VatNumber,
            TinNumber = cmd.TinNumber,
            Category = Enum.Parse<CustomerCategory>(cmd.Category),
            CreditLimit = cmd.CreditLimit,
            CreditPeriodDays = cmd.CreditPeriodDays,
            IsExport = cmd.IsExport,
            ParentCustomerId = cmd.ParentCustomerId,
            Notes = cmd.Notes,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerDto>.Ok(_mapper.Map<CustomerDto>(entity), "Customer created.");
    }
}
