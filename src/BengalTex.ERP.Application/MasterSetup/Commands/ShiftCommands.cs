using BengalTex.ERP.Application.MasterSetup.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.MasterSetup.Commands;

// ── List ──
public sealed record GetShiftsQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<ShiftDto>>>;

internal sealed class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, ApiResponse<IReadOnlyList<ShiftDto>>>
{
    private readonly IRepository<Shift> _repo;
    public GetShiftsQueryHandler(IRepository<Shift> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<ShiftDto>>> Handle(GetShiftsQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(s => s.IsActive);
        var rows = await q.OrderBy(s => s.Code)
            .Select(s => new
            {
                s.Id, s.Code, s.Name, s.StartTime, s.EndTime,
                s.WeekendDayOfWeek, s.SecondWeekendDayOfWeek,
                s.Description, s.IsActive
            }).ToListAsync(ct);
        var items = rows.Select(s => new ShiftDto(
            s.Id, s.Code, s.Name,
            s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"),
            s.WeekendDayOfWeek.ToString(),
            s.SecondWeekendDayOfWeek?.ToString(),
            s.Description, s.IsActive)).ToList();
        return ApiResponse<IReadOnlyList<ShiftDto>>.Ok(items);
    }
}

public sealed record CreateShiftCommand(string Code, string Name,
    string StartTime, string EndTime,
    string WeekendDayOfWeek, string? SecondWeekendDayOfWeek,
    string? Description) : IRequest<ApiResponse<int>>;

public sealed record UpdateShiftCommand(int Id, string Name,
    string StartTime, string EndTime,
    string WeekendDayOfWeek, string? SecondWeekendDayOfWeek,
    string? Description, bool IsActive) : IRequest<ApiResponse<int>>;

public sealed record DeleteShiftCommand(int Id) : IRequest<ApiResponse>;

public sealed class CreateShiftCommandValidator : AbstractValidator<CreateShiftCommand>
{
    public CreateShiftCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartTime).NotEmpty().Must(s => TimeOnly.TryParse(s, out _)).WithMessage("StartTime must be HH:mm.");
        RuleFor(x => x.EndTime).NotEmpty().Must(s => TimeOnly.TryParse(s, out _)).WithMessage("EndTime must be HH:mm.");
        RuleFor(x => x.WeekendDayOfWeek).NotEmpty().Must(s => Enum.TryParse<DayOfWeek>(s, out _));
        RuleFor(x => x.SecondWeekendDayOfWeek)
            .Must(s => string.IsNullOrEmpty(s) || Enum.TryParse<DayOfWeek>(s, out _));
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateShiftCommandValidator : AbstractValidator<UpdateShiftCommand>
{
    public UpdateShiftCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartTime).NotEmpty().Must(s => TimeOnly.TryParse(s, out _));
        RuleFor(x => x.EndTime).NotEmpty().Must(s => TimeOnly.TryParse(s, out _));
        RuleFor(x => x.WeekendDayOfWeek).NotEmpty().Must(s => Enum.TryParse<DayOfWeek>(s, out _));
        RuleFor(x => x.SecondWeekendDayOfWeek)
            .Must(s => string.IsNullOrEmpty(s) || Enum.TryParse<DayOfWeek>(s, out _));
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateShiftCommandHandler : IRequestHandler<CreateShiftCommand, ApiResponse<int>>
{
    private readonly IRepository<Shift> _repo;
    private readonly IUnitOfWork _uow;
    public CreateShiftCommandHandler(IRepository<Shift> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateShiftCommand cmd, CancellationToken ct)
    {
        var code = cmd.Code.Trim().ToUpperInvariant();
        if (await _repo.Query().AnyAsync(s => s.Code == code, ct))
            return ApiResponse<int>.Fail($"Shift '{code}' already exists.");
        var s = new Shift
        {
            Code = code, Name = cmd.Name.Trim(),
            StartTime = TimeOnly.Parse(cmd.StartTime),
            EndTime = TimeOnly.Parse(cmd.EndTime),
            WeekendDayOfWeek = Enum.Parse<DayOfWeek>(cmd.WeekendDayOfWeek),
            SecondWeekendDayOfWeek = string.IsNullOrEmpty(cmd.SecondWeekendDayOfWeek) ? null : Enum.Parse<DayOfWeek>(cmd.SecondWeekendDayOfWeek),
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(s, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(s.Id, "Shift created.");
    }
}

internal sealed class UpdateShiftCommandHandler : IRequestHandler<UpdateShiftCommand, ApiResponse<int>>
{
    private readonly IRepository<Shift> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateShiftCommandHandler(IRepository<Shift> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateShiftCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse<int>.Fail("Shift not found.");
        s.Name = cmd.Name.Trim();
        s.StartTime = TimeOnly.Parse(cmd.StartTime);
        s.EndTime = TimeOnly.Parse(cmd.EndTime);
        s.WeekendDayOfWeek = Enum.Parse<DayOfWeek>(cmd.WeekendDayOfWeek);
        s.SecondWeekendDayOfWeek = string.IsNullOrEmpty(cmd.SecondWeekendDayOfWeek) ? null : Enum.Parse<DayOfWeek>(cmd.SecondWeekendDayOfWeek);
        s.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        s.IsActive = cmd.IsActive;
        _repo.Update(s);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(s.Id, "Shift updated.");
    }
}

internal sealed class DeleteShiftCommandHandler : IRequestHandler<DeleteShiftCommand, ApiResponse>
{
    private readonly IRepository<Shift> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    public DeleteShiftCommandHandler(IRepository<Shift> repo, IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    { _repo = repo; _empRepo = empRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteShiftCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Shift not found.");
        if (await _empRepo.Query().AnyAsync(e => e.ShiftId == cmd.Id, ct))
            return ApiResponse.Fail("This shift is assigned to employees (deactivate instead).");
        _repo.Remove(s);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Shift deleted.");
    }
}
