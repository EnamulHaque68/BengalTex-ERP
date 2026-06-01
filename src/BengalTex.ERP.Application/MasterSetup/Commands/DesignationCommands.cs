using BengalTex.ERP.Application.MasterSetup.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.MasterSetup.Commands;

// ── List ──
public sealed record GetDesignationsQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<DesignationDto>>>;

internal sealed class GetDesignationsQueryHandler : IRequestHandler<GetDesignationsQuery, ApiResponse<IReadOnlyList<DesignationDto>>>
{
    private readonly IRepository<Designation> _repo;
    public GetDesignationsQueryHandler(IRepository<Designation> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<DesignationDto>>> Handle(GetDesignationsQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(d => d.IsActive);
        var items = await q.OrderBy(d => d.GradeLevel).ThenBy(d => d.Name)
            .Select(d => new DesignationDto(d.Id, d.Code, d.Name, d.GradeLevel, d.Description, d.IsActive))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<DesignationDto>>.Ok(items);
    }
}

// ── Create / Update / Delete ──
public sealed record CreateDesignationCommand(string? Code, string Name, int? GradeLevel, string? Description) : IRequest<ApiResponse<int>>;
public sealed record UpdateDesignationCommand(int Id, string? Code, string Name, int? GradeLevel, string? Description, bool IsActive) : IRequest<ApiResponse<int>>;
public sealed record DeleteDesignationCommand(int Id) : IRequest<ApiResponse>;

public sealed class CreateDesignationCommandValidator : AbstractValidator<CreateDesignationCommand>
{
    public CreateDesignationCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GradeLevel).InclusiveBetween(1, 10).When(x => x.GradeLevel.HasValue);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateDesignationCommandValidator : AbstractValidator<UpdateDesignationCommand>
{
    public UpdateDesignationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GradeLevel).InclusiveBetween(1, 10).When(x => x.GradeLevel.HasValue);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateDesignationCommandHandler : IRequestHandler<CreateDesignationCommand, ApiResponse<int>>
{
    private readonly IRepository<Designation> _repo;
    private readonly IUnitOfWork _uow;
    public CreateDesignationCommandHandler(IRepository<Designation> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateDesignationCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        if (await _repo.Query().AnyAsync(d => d.Name == name, ct))
            return ApiResponse<int>.Fail($"Designation '{name}' already exists.");
        var d = new Designation
        {
            Code = string.IsNullOrWhiteSpace(cmd.Code) ? null : cmd.Code.Trim().ToUpperInvariant(),
            Name = name,
            GradeLevel = cmd.GradeLevel,
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(d, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(d.Id, "Designation created.");
    }
}

internal sealed class UpdateDesignationCommandHandler : IRequestHandler<UpdateDesignationCommand, ApiResponse<int>>
{
    private readonly IRepository<Designation> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateDesignationCommandHandler(IRepository<Designation> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateDesignationCommand cmd, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(cmd.Id, ct);
        if (d is null) return ApiResponse<int>.Fail("Designation not found.");
        d.Code = string.IsNullOrWhiteSpace(cmd.Code) ? null : cmd.Code.Trim().ToUpperInvariant();
        d.Name = cmd.Name.Trim();
        d.GradeLevel = cmd.GradeLevel;
        d.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        d.IsActive = cmd.IsActive;
        _repo.Update(d);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(d.Id, "Designation updated.");
    }
}

internal sealed class DeleteDesignationCommandHandler : IRequestHandler<DeleteDesignationCommand, ApiResponse>
{
    private readonly IRepository<Designation> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    public DeleteDesignationCommandHandler(IRepository<Designation> repo, IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    { _repo = repo; _empRepo = empRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteDesignationCommand cmd, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(cmd.Id, ct);
        if (d is null) return ApiResponse.Fail("Designation not found.");
        if (await _empRepo.Query().AnyAsync(e => e.DesignationId == cmd.Id, ct))
            return ApiResponse.Fail("This designation is assigned to employees (deactivate instead).");
        _repo.Remove(d);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Designation deleted.");
    }
}
