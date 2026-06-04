using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.MachineMaintenance.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.MachineMaintenance.Commands;

internal static class MaintenanceMapping
{
    public static MachineMaintenanceDto ToDto(Domain.Entities.MachineMaintenance m, DateOnly today) => new(
        m.Id, m.Code,
        m.MachineId, m.Machine.Code, m.Machine.Name, m.Machine.MachineType, m.Machine.Location,
        m.Type.ToString(), m.Description,
        m.ScheduledDate, m.CompletedDate, m.DowntimeHours,
        m.PerformedBy, m.PerformedByEmployeeId,
        m.PerformedByEmployee != null ? m.PerformedByEmployee.FullName : null,
        m.ServiceCost, m.PartsCost, m.TotalCost,
        m.PartsReplaced, m.CompletionNotes, m.Status.ToString(),
        m.Status == MaintenanceStatus.Scheduled && today > m.ScheduledDate,
        m.IsRecurring, m.IntervalDays, m.RecurringSeriesAnchorId, m.Notes);
}

// ─── List ──────────────────────────────────────────────────────────────────
public sealed record GetMachineMaintenancesQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    string? Type = null,
    int? MachineId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<MachineMaintenanceDto>>>;

internal sealed class GetMachineMaintenancesQueryHandler
    : IRequestHandler<GetMachineMaintenancesQuery, ApiResponse<PagedResult<MachineMaintenanceDto>>>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    public GetMachineMaintenancesQueryHandler(IRepository<Domain.Entities.MachineMaintenance, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<MachineMaintenanceDto>>> Handle(
        GetMachineMaintenancesQuery req, CancellationToken ct)
    {
        var q = _repo.Query()
            .Include(m => m.Machine)
            .Include(m => m.PerformedByEmployee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<MaintenanceStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (!string.IsNullOrEmpty(req.Type) && Enum.TryParse<MaintenanceType>(req.Type, out var t))
            q = q.Where(x => x.Type == t);
        if (req.MachineId.HasValue) q = q.Where(x => x.MachineId == req.MachineId.Value);
        if (req.FromDate.HasValue) q = q.Where(x => x.ScheduledDate >= req.FromDate.Value);
        if (req.ToDate.HasValue) q = q.Where(x => x.ScheduledDate <= req.ToDate.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search)
                          || x.Description.Contains(search)
                          || x.Machine.Code.Contains(search)
                          || x.Machine.Name.Contains(search)
                          || (x.PerformedBy != null && x.PerformedBy.Contains(search)));

        var ordered = q.OrderByDescending(x => x.ScheduledDate).ThenByDescending(x => x.Id);
        var total = await ordered.CountAsync(ct);
        var entities = await ordered
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return ApiResponse<PagedResult<MachineMaintenanceDto>>.Ok(
            PagedResult<MachineMaintenanceDto>.Create(
                entities.Select(m => MaintenanceMapping.ToDto(m, today)).ToList(),
                req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ─── Get By Id ─────────────────────────────────────────────────────────────
public sealed record GetMachineMaintenanceByIdQuery(long Id) : IRequest<ApiResponse<MachineMaintenanceDto>>;

internal sealed class GetMachineMaintenanceByIdQueryHandler
    : IRequestHandler<GetMachineMaintenanceByIdQuery, ApiResponse<MachineMaintenanceDto>>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    public GetMachineMaintenanceByIdQueryHandler(IRepository<Domain.Entities.MachineMaintenance, long> repo) => _repo = repo;

    public async Task<ApiResponse<MachineMaintenanceDto>> Handle(GetMachineMaintenanceByIdQuery q, CancellationToken ct)
    {
        var m = await _repo.Query()
            .Include(x => x.Machine)
            .Include(x => x.PerformedByEmployee)
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);
        return m is null
            ? ApiResponse<MachineMaintenanceDto>.Fail("Maintenance record not found.")
            : ApiResponse<MachineMaintenanceDto>.Ok(MaintenanceMapping.ToDto(m, DateOnly.FromDateTime(DateTime.UtcNow.Date)));
    }
}

// ─── Schedule / Create ─────────────────────────────────────────────────────
public sealed record ScheduleMaintenanceCommand(
    int MachineId,
    string Type,
    string Description,
    DateOnly ScheduledDate,
    bool IsRecurring,
    int? IntervalDays,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class ScheduleMaintenanceCommandValidator : AbstractValidator<ScheduleMaintenanceCommand>
{
    public ScheduleMaintenanceCommandValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0);
        RuleFor(x => x.Type).NotEmpty().Must(v => Enum.TryParse<MaintenanceType>(v, out _));
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ScheduledDate).NotEmpty();
        RuleFor(x => x.IntervalDays)
            .NotNull().GreaterThan(0).LessThanOrEqualTo(3650)
            .When(x => x.IsRecurring)
            .WithMessage("IntervalDays required (1-3650) when IsRecurring = true.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class ScheduleMaintenanceCommandHandler
    : IRequestHandler<ScheduleMaintenanceCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    private readonly IRepository<Machine> _machineRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public ScheduleMaintenanceCommandHandler(
        IRepository<Domain.Entities.MachineMaintenance, long> repo,
        IRepository<Machine> machineRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _machineRepo = machineRepo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(ScheduleMaintenanceCommand cmd, CancellationToken ct)
    {
        if (!await _machineRepo.AnyAsync(m => m.Id == cmd.MachineId && m.IsActive, ct))
            return ApiResponse<long>.Fail("Machine not found or inactive.");

        var code = await _numbering.NextAsync("MM", null, ct);
        var entity = new Domain.Entities.MachineMaintenance
        {
            Code = code,
            MachineId = cmd.MachineId,
            Type = Enum.Parse<MaintenanceType>(cmd.Type),
            Description = cmd.Description.Trim(),
            ScheduledDate = cmd.ScheduledDate,
            Status = MaintenanceStatus.Scheduled,
            IsRecurring = cmd.IsRecurring,
            IntervalDays = cmd.IsRecurring ? cmd.IntervalDays : null,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // Self-anchor the recurring series (so subsequent occurrences point back here)
        if (entity.IsRecurring)
        {
            entity.RecurringSeriesAnchorId = entity.Id;
            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);
        }

        return ApiResponse<long>.Ok(entity.Id, $"Maintenance {entity.Code} scheduled for {cmd.ScheduledDate}.");
    }
}

// ─── Update (Scheduled only) ───────────────────────────────────────────────
public sealed record UpdateMaintenanceCommand(
    long Id,
    string Type,
    string Description,
    DateOnly ScheduledDate,
    bool IsRecurring,
    int? IntervalDays,
    string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateMaintenanceCommandValidator : AbstractValidator<UpdateMaintenanceCommand>
{
    public UpdateMaintenanceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Type).Must(v => Enum.TryParse<MaintenanceType>(v, out _));
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IntervalDays)
            .NotNull().GreaterThan(0).LessThanOrEqualTo(3650)
            .When(x => x.IsRecurring);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateMaintenanceCommandHandler : IRequestHandler<UpdateMaintenanceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateMaintenanceCommandHandler(IRepository<Domain.Entities.MachineMaintenance, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateMaintenanceCommand cmd, CancellationToken ct)
    {
        var m = await _repo.GetByIdAsync(cmd.Id, ct);
        if (m is null) return ApiResponse.Fail("Maintenance record not found.");
        if (m.Status != MaintenanceStatus.Scheduled)
            return ApiResponse.Fail($"Cannot edit a {m.Status} maintenance record.");

        m.Type = Enum.Parse<MaintenanceType>(cmd.Type);
        m.Description = cmd.Description.Trim();
        m.ScheduledDate = cmd.ScheduledDate;
        m.IsRecurring = cmd.IsRecurring;
        m.IntervalDays = cmd.IsRecurring ? cmd.IntervalDays : null;
        m.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(m);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Maintenance record updated.");
    }
}

// ─── Start (Scheduled → InProgress) ────────────────────────────────────────
public sealed record StartMaintenanceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class StartMaintenanceCommandHandler : IRequestHandler<StartMaintenanceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    private readonly IUnitOfWork _uow;
    public StartMaintenanceCommandHandler(IRepository<Domain.Entities.MachineMaintenance, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(StartMaintenanceCommand cmd, CancellationToken ct)
    {
        var m = await _repo.GetByIdAsync(cmd.Id, ct);
        if (m is null) return ApiResponse.Fail("Maintenance record not found.");
        if (m.Status != MaintenanceStatus.Scheduled)
            return ApiResponse.Fail($"Cannot start a {m.Status} maintenance.");
        m.Status = MaintenanceStatus.InProgress;
        _repo.Update(m);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"{m.Code} started.");
    }
}

// ─── Complete (Scheduled / InProgress → Completed; spawns next if recurring) ─
public sealed record CompleteMaintenanceCommand(
    long Id,
    DateOnly CompletedDate,
    decimal? DowntimeHours,
    string? PerformedBy,
    int? PerformedByEmployeeId,
    decimal ServiceCost,
    decimal PartsCost,
    string? PartsReplaced,
    string? CompletionNotes
) : IRequest<ApiResponse<long?>>;   // returns spawned-next-occurrence-id (null if none)

public sealed class CompleteMaintenanceCommandValidator : AbstractValidator<CompleteMaintenanceCommand>
{
    public CompleteMaintenanceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CompletedDate).NotEmpty();
        RuleFor(x => x.DowntimeHours).GreaterThanOrEqualTo(0).When(x => x.DowntimeHours.HasValue);
        RuleFor(x => x.PerformedBy).MaximumLength(150);
        RuleFor(x => x.ServiceCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PartsCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PartsReplaced).MaximumLength(1000);
        RuleFor(x => x.CompletionNotes).MaximumLength(2000);
    }
}

internal sealed class CompleteMaintenanceCommandHandler
    : IRequestHandler<CompleteMaintenanceCommand, ApiResponse<long?>>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CompleteMaintenanceCommandHandler(
        IRepository<Domain.Entities.MachineMaintenance, long> repo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long?>> Handle(CompleteMaintenanceCommand cmd, CancellationToken ct)
    {
        var m = await _repo.GetByIdAsync(cmd.Id, ct);
        if (m is null) return ApiResponse<long?>.Fail("Maintenance record not found.");
        if (m.Status != MaintenanceStatus.Scheduled && m.Status != MaintenanceStatus.InProgress)
            return ApiResponse<long?>.Fail($"Cannot complete a {m.Status} maintenance.");

        m.Status = MaintenanceStatus.Completed;
        m.CompletedDate = cmd.CompletedDate;
        m.DowntimeHours = cmd.DowntimeHours;
        m.PerformedBy = string.IsNullOrWhiteSpace(cmd.PerformedBy) ? null : cmd.PerformedBy.Trim();
        m.PerformedByEmployeeId = cmd.PerformedByEmployeeId;
        m.ServiceCost = cmd.ServiceCost;
        m.PartsCost = cmd.PartsCost;
        m.PartsReplaced = string.IsNullOrWhiteSpace(cmd.PartsReplaced) ? null : cmd.PartsReplaced.Trim();
        m.CompletionNotes = string.IsNullOrWhiteSpace(cmd.CompletionNotes) ? null : cmd.CompletionNotes.Trim();
        _repo.Update(m);

        long? nextId = null;
        if (m.IsRecurring && m.IntervalDays is > 0)
        {
            var nextDate = cmd.CompletedDate.AddDays(m.IntervalDays.Value);
            var nextCode = await _numbering.NextAsync("MM", null, ct);
            var next = new Domain.Entities.MachineMaintenance
            {
                Code = nextCode,
                MachineId = m.MachineId,
                Type = m.Type,
                Description = m.Description,
                ScheduledDate = nextDate,
                Status = MaintenanceStatus.Scheduled,
                IsRecurring = true,
                IntervalDays = m.IntervalDays,
                RecurringSeriesAnchorId = m.RecurringSeriesAnchorId ?? m.Id,
                Notes = m.Notes
            };
            await _repo.AddAsync(next, ct);
            await _uow.SaveChangesAsync(ct);
            nextId = next.Id;
        }
        else
        {
            await _uow.SaveChangesAsync(ct);
        }

        return ApiResponse<long?>.Ok(nextId,
            nextId.HasValue
                ? $"{m.Code} completed. Next occurrence scheduled."
                : $"{m.Code} completed.");
    }
}

// ─── Cancel ────────────────────────────────────────────────────────────────
public sealed record CancelMaintenanceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CancelMaintenanceCommandHandler : IRequestHandler<CancelMaintenanceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    private readonly IUnitOfWork _uow;
    public CancelMaintenanceCommandHandler(IRepository<Domain.Entities.MachineMaintenance, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(CancelMaintenanceCommand cmd, CancellationToken ct)
    {
        var m = await _repo.GetByIdAsync(cmd.Id, ct);
        if (m is null) return ApiResponse.Fail("Maintenance record not found.");
        if (m.Status == MaintenanceStatus.Completed)
            return ApiResponse.Fail("Cannot cancel a completed maintenance.");
        if (m.Status == MaintenanceStatus.Cancelled)
            return ApiResponse.Fail("Maintenance already cancelled.");
        m.Status = MaintenanceStatus.Cancelled;
        _repo.Update(m);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"{m.Code} cancelled.");
    }
}

// ─── Delete (Scheduled only, soft) ─────────────────────────────────────────
public sealed record DeleteMaintenanceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteMaintenanceCommandHandler : IRequestHandler<DeleteMaintenanceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteMaintenanceCommandHandler(IRepository<Domain.Entities.MachineMaintenance, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteMaintenanceCommand cmd, CancellationToken ct)
    {
        var m = await _repo.GetByIdAsync(cmd.Id, ct);
        if (m is null) return ApiResponse.Fail("Maintenance record not found.");
        if (m.Status != MaintenanceStatus.Scheduled)
            return ApiResponse.Fail($"Cannot delete a {m.Status} record. Cancel instead.");
        _repo.Remove(m);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Maintenance record deleted.");
    }
}
