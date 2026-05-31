using BengalTex.ERP.Application.JobCards.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.JobCards.Commands;

// ── List ──
public sealed record GetMachinesQuery(bool IncludeInactive = false) : IRequest<ApiResponse<IReadOnlyList<MachineDto>>>;

internal sealed class GetMachinesQueryHandler : IRequestHandler<GetMachinesQuery, ApiResponse<IReadOnlyList<MachineDto>>>
{
    private readonly IRepository<Machine> _repo;
    public GetMachinesQueryHandler(IRepository<Machine> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<MachineDto>>> Handle(GetMachinesQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(m => m.IsActive);
        var items = await q.OrderBy(m => m.Name)
            .Select(m => new MachineDto(m.Id, m.Code, m.Name, m.MachineType, m.Location, m.CapacityPerHour, m.Notes, m.IsActive))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<MachineDto>>.Ok(items);
    }
}

// ── Create ──
public sealed record CreateMachineCommand(
    string? Code, string Name, string? MachineType, string? Location,
    decimal? CapacityPerHour, string? Notes) : IRequest<ApiResponse<int>>;

public sealed class CreateMachineCommandValidator : AbstractValidator<CreateMachineCommand>
{
    public CreateMachineCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MachineType).MaximumLength(80);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.CapacityPerHour).GreaterThanOrEqualTo(0).When(x => x.CapacityPerHour.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateMachineCommandHandler : IRequestHandler<CreateMachineCommand, ApiResponse<int>>
{
    private readonly IRepository<Machine> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    public CreateMachineCommandHandler(IRepository<Machine> repo, IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<int>> Handle(CreateMachineCommand cmd, CancellationToken ct)
    {
        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("MC", null, ct)
            : cmd.Code.Trim().ToUpperInvariant();

        if (await _repo.Query().AnyAsync(m => m.Code == code, ct))
            return ApiResponse<int>.Fail($"Machine code '{code}' already exists.");

        var e = new Machine
        {
            Code = code,
            Name = cmd.Name.Trim(),
            MachineType = string.IsNullOrWhiteSpace(cmd.MachineType) ? null : cmd.MachineType.Trim(),
            Location = string.IsNullOrWhiteSpace(cmd.Location) ? null : cmd.Location.Trim(),
            CapacityPerHour = cmd.CapacityPerHour,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Machine created.");
    }
}

// ── Update ──
public sealed record UpdateMachineCommand(
    int Id, string Name, string? MachineType, string? Location,
    decimal? CapacityPerHour, string? Notes, bool IsActive) : IRequest<ApiResponse<int>>;

public sealed class UpdateMachineCommandValidator : AbstractValidator<UpdateMachineCommand>
{
    public UpdateMachineCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MachineType).MaximumLength(80);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.CapacityPerHour).GreaterThanOrEqualTo(0).When(x => x.CapacityPerHour.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateMachineCommandHandler : IRequestHandler<UpdateMachineCommand, ApiResponse<int>>
{
    private readonly IRepository<Machine> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateMachineCommandHandler(IRepository<Machine> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateMachineCommand cmd, CancellationToken ct)
    {
        var m = await _repo.GetByIdAsync(cmd.Id, ct);
        if (m is null) return ApiResponse<int>.Fail("Machine not found.");
        m.Name = cmd.Name.Trim();
        m.MachineType = string.IsNullOrWhiteSpace(cmd.MachineType) ? null : cmd.MachineType.Trim();
        m.Location = string.IsNullOrWhiteSpace(cmd.Location) ? null : cmd.Location.Trim();
        m.CapacityPerHour = cmd.CapacityPerHour;
        m.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        m.IsActive = cmd.IsActive;
        _repo.Update(m);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(m.Id, "Machine updated.");
    }
}

// ── Delete ──
public sealed record DeleteMachineCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteMachineCommandHandler : IRequestHandler<DeleteMachineCommand, ApiResponse>
{
    private readonly IRepository<Machine> _repo;
    private readonly IRepository<JobCard, long> _jcRepo;
    private readonly IUnitOfWork _uow;
    public DeleteMachineCommandHandler(IRepository<Machine> repo, IRepository<JobCard, long> jcRepo, IUnitOfWork uow)
    { _repo = repo; _jcRepo = jcRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteMachineCommand cmd, CancellationToken ct)
    {
        var m = await _repo.GetByIdAsync(cmd.Id, ct);
        if (m is null) return ApiResponse.Fail("Machine not found.");
        if (await _jcRepo.Query().AnyAsync(j => j.MachineId == cmd.Id, ct))
            return ApiResponse.Fail("This machine is used by job cards (deactivate it instead).");
        _repo.Remove(m);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Machine deleted.");
    }
}
