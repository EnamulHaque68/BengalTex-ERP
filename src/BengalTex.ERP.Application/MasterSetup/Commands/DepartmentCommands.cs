using BengalTex.ERP.Application.MasterSetup.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.MasterSetup.Commands;

// ── List ──
public sealed record GetDepartmentsQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<DepartmentDto>>>;

internal sealed class GetDepartmentsQueryHandler
    : IRequestHandler<GetDepartmentsQuery, ApiResponse<IReadOnlyList<DepartmentDto>>>
{
    private readonly IRepository<Department> _repo;
    public GetDepartmentsQueryHandler(IRepository<Department> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<DepartmentDto>>> Handle(GetDepartmentsQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(d => d.IsActive);
        var items = await q.OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id, d.Code, d.Name,
                d.ParentDepartmentId,
                d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                d.HeadEmployeeId,
                d.HeadEmployee != null ? d.HeadEmployee.FullName : null,
                d.Description, d.IsActive))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(items);
    }
}

// ── Create ──
public sealed record CreateDepartmentCommand(
    string? Code, string Name, int? ParentDepartmentId, int? HeadEmployeeId, string? Description
) : IRequest<ApiResponse<int>>;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, ApiResponse<int>>
{
    private readonly IRepository<Department> _repo;
    private readonly IUnitOfWork _uow;
    public CreateDepartmentCommandHandler(IRepository<Department> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateDepartmentCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        if (await _repo.Query().AnyAsync(d => d.Name == name, ct))
            return ApiResponse<int>.Fail($"Department '{name}' already exists.");
        var d = new Department
        {
            Code = string.IsNullOrWhiteSpace(cmd.Code) ? null : cmd.Code.Trim().ToUpperInvariant(),
            Name = name,
            ParentDepartmentId = cmd.ParentDepartmentId,
            HeadEmployeeId = cmd.HeadEmployeeId,
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(d, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(d.Id, "Department created.");
    }
}

// ── Update ──
public sealed record UpdateDepartmentCommand(
    int Id, string? Code, string Name, int? ParentDepartmentId, int? HeadEmployeeId,
    string? Description, bool IsActive
) : IRequest<ApiResponse<int>>;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, ApiResponse<int>>
{
    private readonly IRepository<Department> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateDepartmentCommandHandler(IRepository<Department> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateDepartmentCommand cmd, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(cmd.Id, ct);
        if (d is null) return ApiResponse<int>.Fail("Department not found.");
        if (cmd.ParentDepartmentId == cmd.Id)
            return ApiResponse<int>.Fail("Department cannot be its own parent.");
        d.Code = string.IsNullOrWhiteSpace(cmd.Code) ? null : cmd.Code.Trim().ToUpperInvariant();
        d.Name = cmd.Name.Trim();
        d.ParentDepartmentId = cmd.ParentDepartmentId;
        d.HeadEmployeeId = cmd.HeadEmployeeId;
        d.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        d.IsActive = cmd.IsActive;
        _repo.Update(d);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(d.Id, "Department updated.");
    }
}

// ── Delete ──
public sealed record DeleteDepartmentCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, ApiResponse>
{
    private readonly IRepository<Department> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    public DeleteDepartmentCommandHandler(IRepository<Department> repo, IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    { _repo = repo; _empRepo = empRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteDepartmentCommand cmd, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(cmd.Id, ct);
        if (d is null) return ApiResponse.Fail("Department not found.");
        if (await _empRepo.Query().AnyAsync(e => e.DepartmentId == cmd.Id, ct))
            return ApiResponse.Fail("This department is assigned to employees (deactivate instead).");
        if (await _repo.Query().AnyAsync(x => x.ParentDepartmentId == cmd.Id, ct))
            return ApiResponse.Fail("This department has children (reassign or remove them first).");
        _repo.Remove(d);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Department deleted.");
    }
}
