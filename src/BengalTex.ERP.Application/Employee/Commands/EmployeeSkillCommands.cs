using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Commands;

// ── List ──
public sealed record GetEmployeeSkillsQuery(int EmployeeId) : IRequest<ApiResponse<IReadOnlyList<ProfileSkillDto>>>;

internal sealed class GetEmployeeSkillsQueryHandler : IRequestHandler<GetEmployeeSkillsQuery, ApiResponse<IReadOnlyList<ProfileSkillDto>>>
{
    private readonly IRepository<EmployeeSkill> _repo;
    public GetEmployeeSkillsQueryHandler(IRepository<EmployeeSkill> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<ProfileSkillDto>>> Handle(GetEmployeeSkillsQuery req, CancellationToken ct)
    {
        var list = await _repo.Query().AsNoTracking()
            .Where(s => s.EmployeeId == req.EmployeeId)
            .OrderBy(s => s.SortOrder).ThenByDescending(s => s.ProficiencyPercent)
            .Select(s => new ProfileSkillDto(s.Id, s.Name, s.ProficiencyPercent))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<ProfileSkillDto>>.Ok(list);
    }
}

// ── Create ──
public sealed record CreateEmployeeSkillCommand(int EmployeeId, string Name, int ProficiencyPercent) : IRequest<ApiResponse<int>>;

public sealed class CreateEmployeeSkillCommandValidator : AbstractValidator<CreateEmployeeSkillCommand>
{
    public CreateEmployeeSkillCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProficiencyPercent).InclusiveBetween(0, 100);
    }
}

internal sealed class CreateEmployeeSkillCommandHandler : IRequestHandler<CreateEmployeeSkillCommand, ApiResponse<int>>
{
    private readonly IRepository<EmployeeSkill> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    public CreateEmployeeSkillCommandHandler(IRepository<EmployeeSkill> repo, IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    { _repo = repo; _empRepo = empRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateEmployeeSkillCommand cmd, CancellationToken ct)
    {
        if (await _empRepo.GetByIdAsync(cmd.EmployeeId, ct) is null) return ApiResponse<int>.Fail("Employee not found.");
        var count = await _repo.Query().CountAsync(s => s.EmployeeId == cmd.EmployeeId, ct);
        var e = new EmployeeSkill { EmployeeId = cmd.EmployeeId, Name = cmd.Name.Trim(), ProficiencyPercent = cmd.ProficiencyPercent, SortOrder = count };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Skill added.");
    }
}

// ── Update ──
public sealed record UpdateEmployeeSkillCommand(int Id, string Name, int ProficiencyPercent) : IRequest<ApiResponse<int>>;

public sealed class UpdateEmployeeSkillCommandValidator : AbstractValidator<UpdateEmployeeSkillCommand>
{
    public UpdateEmployeeSkillCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProficiencyPercent).InclusiveBetween(0, 100);
    }
}

internal sealed class UpdateEmployeeSkillCommandHandler : IRequestHandler<UpdateEmployeeSkillCommand, ApiResponse<int>>
{
    private readonly IRepository<EmployeeSkill> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateEmployeeSkillCommandHandler(IRepository<EmployeeSkill> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateEmployeeSkillCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<int>.Fail("Skill not found.");
        e.Name = cmd.Name.Trim();
        e.ProficiencyPercent = cmd.ProficiencyPercent;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Skill updated.");
    }
}

// ── Delete ──
public sealed record DeleteEmployeeSkillCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteEmployeeSkillCommandHandler : IRequestHandler<DeleteEmployeeSkillCommand, ApiResponse>
{
    private readonly IRepository<EmployeeSkill> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteEmployeeSkillCommandHandler(IRepository<EmployeeSkill> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteEmployeeSkillCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Skill not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Skill removed.");
    }
}
