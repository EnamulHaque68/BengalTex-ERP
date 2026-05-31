using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Employee.Commands;

public sealed record UpdateEmployeeCommand(
    int Id,
    string FullName,
    string? Designation,
    string? Department,
    string? Phone,
    string? Email,
    string? NationalId,
    string? Address,
    DateOnly JoiningDate,
    DateOnly? DateOfBirth,
    string Gender,
    string EmploymentType,
    decimal BasicSalary,
    decimal HouseRentAllowance,
    decimal MedicalAllowance,
    decimal TransportAllowance,
    decimal FoodAllowance,
    bool IsPfMember,
    decimal PfRate,
    bool IsTaxable,
    string Status,                 // "Active" | "Inactive" | "Terminated"
    string? Notes,
    bool IsActive
) : IRequest<ApiResponse<EmployeeDto>>;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => Enum.TryParse<EmployeeStatus>(s, out _))
            .WithMessage("Status must be Active, Inactive, or Terminated.");
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.HouseRentAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MedicalAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TransportAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FoodAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PfRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateEmployeeCommandHandler
    : IRequestHandler<UpdateEmployeeCommand, ApiResponse<EmployeeDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateEmployeeCommandHandler(
        IRepository<Domain.Entities.Employee> repo, IUnitOfWork uow, IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<EmployeeDto>> Handle(
        UpdateEmployeeCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<EmployeeDto>.Fail("Employee not found.");

        entity.FullName = cmd.FullName;
        entity.Designation = cmd.Designation;
        entity.Department = cmd.Department;
        entity.Phone = cmd.Phone;
        entity.Email = cmd.Email;
        entity.NationalId = cmd.NationalId;
        entity.Address = cmd.Address;
        entity.JoiningDate = cmd.JoiningDate;
        entity.DateOfBirth = cmd.DateOfBirth;
        entity.Gender = Enum.Parse<Gender>(cmd.Gender);
        entity.EmploymentType = Enum.Parse<EmploymentType>(cmd.EmploymentType);
        entity.BasicSalary = cmd.BasicSalary;
        entity.HouseRentAllowance = cmd.HouseRentAllowance;
        entity.MedicalAllowance = cmd.MedicalAllowance;
        entity.TransportAllowance = cmd.TransportAllowance;
        entity.FoodAllowance = cmd.FoodAllowance;
        entity.IsPfMember = cmd.IsPfMember;
        entity.PfRate = cmd.PfRate;
        entity.IsTaxable = cmd.IsTaxable;
        entity.Status = Enum.Parse<EmployeeStatus>(cmd.Status);
        entity.Notes = cmd.Notes;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(entity), "Employee updated.");
    }
}
