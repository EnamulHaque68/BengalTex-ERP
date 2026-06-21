using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Commands;

// ════════════════════════ Education ════════════════════════
public sealed record GetEmployeeEducationQuery(int EmployeeId) : IRequest<ApiResponse<IReadOnlyList<ProfileEducationDto>>>;

internal sealed class GetEmployeeEducationQueryHandler : IRequestHandler<GetEmployeeEducationQuery, ApiResponse<IReadOnlyList<ProfileEducationDto>>>
{
    private readonly IRepository<EmployeeEducation> _repo;
    public GetEmployeeEducationQueryHandler(IRepository<EmployeeEducation> repo) => _repo = repo;
    public async Task<ApiResponse<IReadOnlyList<ProfileEducationDto>>> Handle(GetEmployeeEducationQuery req, CancellationToken ct)
    {
        var list = await _repo.Query().AsNoTracking().Where(x => x.EmployeeId == req.EmployeeId)
            .OrderBy(x => x.SortOrder).ThenByDescending(x => x.PassingYear)
            .Select(x => new ProfileEducationDto(x.Id, x.Degree, x.Institute, x.PassingYear, x.Result)).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<ProfileEducationDto>>.Ok(list);
    }
}

public sealed record SaveEmployeeEducationCommand(
    int Id, int EmployeeId, string Degree, string? Institute, int? PassingYear, string? Result) : IRequest<ApiResponse<int>>;

public sealed class SaveEmployeeEducationCommandValidator : AbstractValidator<SaveEmployeeEducationCommand>
{
    public SaveEmployeeEducationCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Institute).MaximumLength(200);
        RuleFor(x => x.Result).MaximumLength(100);
        RuleFor(x => x.PassingYear).InclusiveBetween(1950, 2100).When(x => x.PassingYear.HasValue);
    }
}

internal sealed class SaveEmployeeEducationCommandHandler : IRequestHandler<SaveEmployeeEducationCommand, ApiResponse<int>>
{
    private readonly IRepository<EmployeeEducation> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    public SaveEmployeeEducationCommandHandler(IRepository<EmployeeEducation> repo, IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    { _repo = repo; _empRepo = empRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(SaveEmployeeEducationCommand cmd, CancellationToken ct)
    {
        EmployeeEducation e;
        if (cmd.Id > 0)
        {
            var existing = await _repo.GetByIdAsync(cmd.Id, ct);
            if (existing is null) return ApiResponse<int>.Fail("Education record not found.");
            e = existing;
        }
        else
        {
            if (await _empRepo.GetByIdAsync(cmd.EmployeeId, ct) is null) return ApiResponse<int>.Fail("Employee not found.");
            e = new EmployeeEducation { EmployeeId = cmd.EmployeeId, SortOrder = await _repo.Query().CountAsync(x => x.EmployeeId == cmd.EmployeeId, ct) };
            await _repo.AddAsync(e, ct);
        }
        e.Degree = cmd.Degree.Trim();
        e.Institute = string.IsNullOrWhiteSpace(cmd.Institute) ? null : cmd.Institute.Trim();
        e.PassingYear = cmd.PassingYear;
        e.Result = string.IsNullOrWhiteSpace(cmd.Result) ? null : cmd.Result.Trim();
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Education saved.");
    }
}

public sealed record DeleteEmployeeEducationCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteEmployeeEducationCommandHandler : IRequestHandler<DeleteEmployeeEducationCommand, ApiResponse>
{
    private readonly IRepository<EmployeeEducation> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteEmployeeEducationCommandHandler(IRepository<EmployeeEducation> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<ApiResponse> Handle(DeleteEmployeeEducationCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Education record not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Education removed.");
    }
}

// ════════════════════ Emergency Contact ════════════════════
public sealed record GetEmployeeContactsQuery(int EmployeeId) : IRequest<ApiResponse<IReadOnlyList<ProfileEmergencyContactDto>>>;

internal sealed class GetEmployeeContactsQueryHandler : IRequestHandler<GetEmployeeContactsQuery, ApiResponse<IReadOnlyList<ProfileEmergencyContactDto>>>
{
    private readonly IRepository<EmployeeEmergencyContact> _repo;
    public GetEmployeeContactsQueryHandler(IRepository<EmployeeEmergencyContact> repo) => _repo = repo;
    public async Task<ApiResponse<IReadOnlyList<ProfileEmergencyContactDto>>> Handle(GetEmployeeContactsQuery req, CancellationToken ct)
    {
        var list = await _repo.Query().AsNoTracking().Where(x => x.EmployeeId == req.EmployeeId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ProfileEmergencyContactDto(x.Id, x.Name, x.Relationship, x.Phone, x.Address)).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<ProfileEmergencyContactDto>>.Ok(list);
    }
}

public sealed record SaveEmployeeContactCommand(
    int Id, int EmployeeId, string Name, string? Relationship, string Phone, string? Address) : IRequest<ApiResponse<int>>;

public sealed class SaveEmployeeContactCommandValidator : AbstractValidator<SaveEmployeeContactCommand>
{
    public SaveEmployeeContactCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Relationship).MaximumLength(50);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

internal sealed class SaveEmployeeContactCommandHandler : IRequestHandler<SaveEmployeeContactCommand, ApiResponse<int>>
{
    private readonly IRepository<EmployeeEmergencyContact> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    public SaveEmployeeContactCommandHandler(IRepository<EmployeeEmergencyContact> repo, IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    { _repo = repo; _empRepo = empRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(SaveEmployeeContactCommand cmd, CancellationToken ct)
    {
        EmployeeEmergencyContact e;
        if (cmd.Id > 0)
        {
            var existing = await _repo.GetByIdAsync(cmd.Id, ct);
            if (existing is null) return ApiResponse<int>.Fail("Contact not found.");
            e = existing;
        }
        else
        {
            if (await _empRepo.GetByIdAsync(cmd.EmployeeId, ct) is null) return ApiResponse<int>.Fail("Employee not found.");
            e = new EmployeeEmergencyContact { EmployeeId = cmd.EmployeeId, SortOrder = await _repo.Query().CountAsync(x => x.EmployeeId == cmd.EmployeeId, ct) };
            await _repo.AddAsync(e, ct);
        }
        e.Name = cmd.Name.Trim();
        e.Relationship = string.IsNullOrWhiteSpace(cmd.Relationship) ? null : cmd.Relationship.Trim();
        e.Phone = cmd.Phone.Trim();
        e.Address = string.IsNullOrWhiteSpace(cmd.Address) ? null : cmd.Address.Trim();
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Contact saved.");
    }
}

public sealed record DeleteEmployeeContactCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteEmployeeContactCommandHandler : IRequestHandler<DeleteEmployeeContactCommand, ApiResponse>
{
    private readonly IRepository<EmployeeEmergencyContact> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteEmployeeContactCommandHandler(IRepository<EmployeeEmergencyContact> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<ApiResponse> Handle(DeleteEmployeeContactCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Contact not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Contact removed.");
    }
}
