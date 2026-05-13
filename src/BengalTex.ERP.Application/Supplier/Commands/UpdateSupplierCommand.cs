using BengalTex.ERP.Application.Supplier.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Supplier.Commands;

public sealed record UpdateSupplierCommand(
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
    int PaymentTermsDays,
    string? BankName,
    string? BankAccountNumber,
    string? BankBranch,
    string? BankAccountHolderName,
    int Rating,
    string? Notes,
    bool IsActive
) : IRequest<ApiResponse<SupplierDto>>;

public sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
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
        RuleFor(x => x.PaymentTermsDays).InclusiveBetween(0, 365);
        RuleFor(x => x.BankName).MaximumLength(100);
        RuleFor(x => x.BankAccountNumber).MaximumLength(50);
        RuleFor(x => x.BankBranch).MaximumLength(100);
        RuleFor(x => x.BankAccountHolderName).MaximumLength(200);
        RuleFor(x => x.Rating).InclusiveBetween(0, 5);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateSupplierCommandHandler
    : IRequestHandler<UpdateSupplierCommand, ApiResponse<SupplierDto>>
{
    private readonly IRepository<Domain.Entities.Supplier> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateSupplierCommandHandler(
        IRepository<Domain.Entities.Supplier> repo,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<SupplierDto>> Handle(
        UpdateSupplierCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<SupplierDto>.Fail("Supplier not found.");

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
        entity.PaymentTermsDays = cmd.PaymentTermsDays;
        entity.BankName = cmd.BankName;
        entity.BankAccountNumber = cmd.BankAccountNumber;
        entity.BankBranch = cmd.BankBranch;
        entity.BankAccountHolderName = cmd.BankAccountHolderName;
        entity.Rating = cmd.Rating;
        entity.Notes = cmd.Notes;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierDto>.Ok(_mapper.Map<SupplierDto>(entity), "Supplier updated.");
    }
}
