using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.WorkCenters.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.WorkCenters.Commands;

// ── List (with capacity load) ──
public sealed record GetWorkCentersQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<WorkCenterDto>>>;

internal sealed class GetWorkCentersQueryHandler
    : IRequestHandler<GetWorkCentersQuery, ApiResponse<IReadOnlyList<WorkCenterDto>>>
{
    private readonly IRepository<WorkCenter> _repo;
    private readonly IRepository<ProductionStage, long> _stageRepo;

    public GetWorkCentersQueryHandler(IRepository<WorkCenter> repo, IRepository<ProductionStage, long> stageRepo)
    {
        _repo = repo;
        _stageRepo = stageRepo;
    }

    public async Task<ApiResponse<IReadOnlyList<WorkCenterDto>>> Handle(GetWorkCentersQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(w => w.IsActive);
        var centers = await q.OrderBy(w => w.Name).ToListAsync(ct);

        // Planned load = Σ planned qty of open stages (order Draft/InProgress, stage Pending/InProgress)
        // assigned to a work center. Materialize-then-group to avoid nested-aggregate translation issues.
        var openStages = await _stageRepo.Query()
            .Where(s => s.WorkCenterId != null
                && (s.Status == ProductionStageStatus.Pending || s.Status == ProductionStageStatus.InProgress)
                && (s.ProductionOrder.Status == ProductionOrderStatus.Draft
                    || s.ProductionOrder.Status == ProductionOrderStatus.InProgress))
            .Select(s => new { WcId = s.WorkCenterId!.Value, s.PlannedQuantity })
            .ToListAsync(ct);

        var loadByWc = openStages
            .GroupBy(x => x.WcId)
            .ToDictionary(g => g.Key, g => (Load: g.Sum(x => x.PlannedQuantity), Count: g.Count()));

        var items = centers.Select(w =>
        {
            var (load, count) = loadByWc.TryGetValue(w.Id, out var v) ? v : (0m, 0);
            decimal? loadPercent = w.CapacityPerDay is > 0m
                ? Math.Round(load / w.CapacityPerDay.Value * 100m, 1)
                : null;
            return new WorkCenterDto(
                w.Id, w.Code, w.Name, w.Type, w.Location,
                w.CapacityPerDay, w.CostPerHour, w.Notes, w.IsActive,
                load, count, loadPercent);
        }).ToList();

        return ApiResponse<IReadOnlyList<WorkCenterDto>>.Ok(items);
    }
}

// ── Create ──
public sealed record CreateWorkCenterCommand(
    string Code, string Name, string? Type, string? Location,
    decimal? CapacityPerDay, decimal? CostPerHour, string? Notes) : IRequest<ApiResponse<int>>;

public sealed class CreateWorkCenterCommandValidator : AbstractValidator<CreateWorkCenterCommand>
{
    public CreateWorkCenterCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).MaximumLength(80);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.CapacityPerDay).GreaterThanOrEqualTo(0).When(x => x.CapacityPerDay.HasValue);
        RuleFor(x => x.CostPerHour).GreaterThanOrEqualTo(0).When(x => x.CostPerHour.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateWorkCenterCommandHandler : IRequestHandler<CreateWorkCenterCommand, ApiResponse<int>>
{
    private readonly IRepository<WorkCenter> _repo;
    private readonly IUnitOfWork _uow;
    public CreateWorkCenterCommandHandler(IRepository<WorkCenter> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateWorkCenterCommand cmd, CancellationToken ct)
    {
        var code = cmd.Code.Trim().ToUpperInvariant();
        if (await _repo.Query().AnyAsync(w => w.Code == code, ct))
            return ApiResponse<int>.Fail($"Work center code '{code}' already exists.");

        var e = new WorkCenter
        {
            Code = code,
            Name = cmd.Name.Trim(),
            Type = string.IsNullOrWhiteSpace(cmd.Type) ? null : cmd.Type.Trim(),
            Location = string.IsNullOrWhiteSpace(cmd.Location) ? null : cmd.Location.Trim(),
            CapacityPerDay = cmd.CapacityPerDay,
            CostPerHour = cmd.CostPerHour,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Work center created.");
    }
}

// ── Update ──
public sealed record UpdateWorkCenterCommand(
    int Id, string Name, string? Type, string? Location,
    decimal? CapacityPerDay, decimal? CostPerHour, string? Notes, bool IsActive) : IRequest<ApiResponse<int>>;

public sealed class UpdateWorkCenterCommandValidator : AbstractValidator<UpdateWorkCenterCommand>
{
    public UpdateWorkCenterCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).MaximumLength(80);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.CapacityPerDay).GreaterThanOrEqualTo(0).When(x => x.CapacityPerDay.HasValue);
        RuleFor(x => x.CostPerHour).GreaterThanOrEqualTo(0).When(x => x.CostPerHour.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateWorkCenterCommandHandler : IRequestHandler<UpdateWorkCenterCommand, ApiResponse<int>>
{
    private readonly IRepository<WorkCenter> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateWorkCenterCommandHandler(IRepository<WorkCenter> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateWorkCenterCommand cmd, CancellationToken ct)
    {
        var w = await _repo.GetByIdAsync(cmd.Id, ct);
        if (w is null) return ApiResponse<int>.Fail("Work center not found.");
        w.Name = cmd.Name.Trim();
        w.Type = string.IsNullOrWhiteSpace(cmd.Type) ? null : cmd.Type.Trim();
        w.Location = string.IsNullOrWhiteSpace(cmd.Location) ? null : cmd.Location.Trim();
        w.CapacityPerDay = cmd.CapacityPerDay;
        w.CostPerHour = cmd.CostPerHour;
        w.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        w.IsActive = cmd.IsActive;
        _repo.Update(w);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(w.Id, "Work center updated.");
    }
}

// ── Delete ──
public sealed record DeleteWorkCenterCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteWorkCenterCommandHandler : IRequestHandler<DeleteWorkCenterCommand, ApiResponse>
{
    private readonly IRepository<WorkCenter> _repo;
    private readonly IRepository<ProductionStage, long> _stageRepo;
    private readonly IUnitOfWork _uow;
    public DeleteWorkCenterCommandHandler(
        IRepository<WorkCenter> repo, IRepository<ProductionStage, long> stageRepo, IUnitOfWork uow)
    { _repo = repo; _stageRepo = stageRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteWorkCenterCommand cmd, CancellationToken ct)
    {
        var w = await _repo.GetByIdAsync(cmd.Id, ct);
        if (w is null) return ApiResponse.Fail("Work center not found.");
        if (await _stageRepo.Query().AnyAsync(s => s.WorkCenterId == cmd.Id, ct))
            return ApiResponse.Fail("This work center is used by production stages (deactivate it instead).");
        _repo.Remove(w);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Work center deleted.");
    }
}
