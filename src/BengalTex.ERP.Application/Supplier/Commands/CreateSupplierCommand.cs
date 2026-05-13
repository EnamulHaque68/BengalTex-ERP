using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.Supplier.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Supplier.Commands;

public sealed record CreateSupplierCommand(
    string? Code,                  // null → auto-gen via NumberingService("SUPP")
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
    int PaymentTermsDays,
    string? BankName,
    string? BankAccountNumber,
    string? BankBranch,
    string? BankAccountHolderName,
    int Rating,
    string? Notes
) : IRequest<ApiResponse<SupplierDto>>;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
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

        RuleFor(x => x.PaymentTermsDays).InclusiveBetween(0, 365);

        RuleFor(x => x.BankName).MaximumLength(100);
        RuleFor(x => x.BankAccountNumber).MaximumLength(50);
        RuleFor(x => x.BankBranch).MaximumLength(100);
        RuleFor(x => x.BankAccountHolderName).MaximumLength(200);

        RuleFor(x => x.Rating).InclusiveBetween(0, 5);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateSupplierCommandHandler
    : IRequestHandler<CreateSupplierCommand, ApiResponse<SupplierDto>>
{
    private readonly IRepository<Domain.Entities.Supplier> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INumberingService _numbering;

    public CreateSupplierCommandHandler(
        IRepository<Domain.Entities.Supplier> repo,
        IUnitOfWork uow,
        IMapper mapper,
        INumberingService numbering)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
        _numbering = numbering;
    }

    public async Task<ApiResponse<SupplierDto>> Handle(
        CreateSupplierCommand cmd, CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("SUPP", null, cancellationToken)
            : cmd.Code.Trim().ToUpperInvariant();

        if (await _repo.AnyAsync(s => s.Code == code, cancellationToken))
            return ApiResponse<SupplierDto>.Fail($"Supplier code '{code}' already exists.");

        var entity = new Domain.Entities.Supplier
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
            PaymentTermsDays = cmd.PaymentTermsDays,
            BankName = cmd.BankName,
            BankAccountNumber = cmd.BankAccountNumber,
            BankBranch = cmd.BankBranch,
            BankAccountHolderName = cmd.BankAccountHolderName,
            Rating = cmd.Rating,
            Notes = cmd.Notes,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierDto>.Ok(_mapper.Map<SupplierDto>(entity), "Supplier created.");
    }
}
