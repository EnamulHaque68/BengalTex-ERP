using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Employee.Commands;

public sealed record CreateEmployeeCommand(
    string? Code,                  // optional — auto-generated via NumberingService("EMP")
    string FullName,
    string? Designation,
    string? Department,
    string? Phone,
    string? Email,
    string? NationalId,
    string? Address,
    DateOnly JoiningDate,
    DateOnly? DateOfBirth,
    string Gender,                 // "Male" | "Female" | "Other"
    string EmploymentType,         // "Permanent" | "Contract" | "DailyWage"
    decimal BasicSalary,
    decimal HouseRentAllowance,
    decimal MedicalAllowance,
    decimal TransportAllowance,
    decimal FoodAllowance,
    bool IsPfMember,
    decimal PfRate,
    bool IsTaxable,
    int? DepartmentId,
    int? DesignationId,
    int? ShiftId,
    int? BankAccountId,
    string? Notes,
    int? ReportingToEmployeeId = null    // line manager / supervisor
) : IRequest<ApiResponse<EmployeeDto>>;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50)
            .Matches("^[A-Z0-9/_-]+$")
                .When(x => !string.IsNullOrEmpty(x.Code))
                .WithMessage("Code must contain uppercase letters, digits, slash, hyphen, underscore.");

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Designation).MaximumLength(100);
        RuleFor(x => x.Department).MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email)).MaximumLength(200);
        RuleFor(x => x.NationalId).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.JoiningDate).NotEmpty();
        RuleFor(x => x.Gender).NotEmpty()
            .Must(g => Enum.TryParse<Gender>(g, out _)).WithMessage("Gender must be Male, Female, or Other.");
        RuleFor(x => x.EmploymentType).NotEmpty()
            .Must(t => Enum.TryParse<EmploymentType>(t, out _))
            .WithMessage("EmploymentType must be Permanent, Contract, or DailyWage.");
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.HouseRentAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MedicalAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TransportAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FoodAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PfRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateEmployeeCommandHandler
    : IRequestHandler<CreateEmployeeCommand, ApiResponse<EmployeeDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INumberingService _numbering;

    public CreateEmployeeCommandHandler(
        IRepository<Domain.Entities.Employee> repo,
        IUnitOfWork uow,
        IMapper mapper,
        INumberingService numbering)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
        _numbering = numbering;
    }

    public async Task<ApiResponse<EmployeeDto>> Handle(
        CreateEmployeeCommand cmd, CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("EMP", null, cancellationToken)
            : cmd.Code.Trim().ToUpperInvariant();

        if (await _repo.AnyAsync(e => e.Code == code, cancellationToken))
            return ApiResponse<EmployeeDto>.Fail($"Employee code '{code}' already exists.");

        if (cmd.ReportingToEmployeeId is int rid && !await _repo.AnyAsync(e => e.Id == rid, cancellationToken))
            return ApiResponse<EmployeeDto>.Fail("The selected supervisor (Reporting To) doesn't exist.");

        var entity = new Domain.Entities.Employee
        {
            Code = code,
            FullName = cmd.FullName,
            Designation = cmd.Designation,
            Department = cmd.Department,
            Phone = cmd.Phone,
            Email = cmd.Email,
            NationalId = cmd.NationalId,
            Address = cmd.Address,
            JoiningDate = cmd.JoiningDate,
            DateOfBirth = cmd.DateOfBirth,
            Gender = Enum.Parse<Gender>(cmd.Gender),
            EmploymentType = Enum.Parse<EmploymentType>(cmd.EmploymentType),
            BasicSalary = cmd.BasicSalary,
            HouseRentAllowance = cmd.HouseRentAllowance,
            MedicalAllowance = cmd.MedicalAllowance,
            TransportAllowance = cmd.TransportAllowance,
            FoodAllowance = cmd.FoodAllowance,
            IsPfMember = cmd.IsPfMember,
            PfRate = cmd.PfRate,
            IsTaxable = cmd.IsTaxable,
            DepartmentId = cmd.DepartmentId,
            DesignationId = cmd.DesignationId,
            ShiftId = cmd.ShiftId,
            BankAccountId = cmd.BankAccountId,
            ReportingToEmployeeId = cmd.ReportingToEmployeeId,
            Status = EmployeeStatus.Active,
            Notes = cmd.Notes,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(entity), "Employee created.");
    }
}
