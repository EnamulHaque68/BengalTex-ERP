using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Commands;

/// <summary>Start a break (Break Out) for the logged-in user's today record.</summary>
public sealed record BreakOutCommand : IRequest<ApiResponse<long>>;

internal sealed class BreakOutCommandHandler : IRequestHandler<BreakOutCommand, ApiResponse<long>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public BreakOutCommandHandler(IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IUnitOfWork uow, ICurrentUserService currentUser, IDateTimeProvider clock)
    { _repo = repo; _employeeRepo = employeeRepo; _uow = uow; _currentUser = currentUser; _clock = clock; }

    public async Task<ApiResponse<long>> Handle(BreakOutCommand cmd, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null) return ApiResponse<long>.Fail("Your login isn't linked to an active employee.");

        var today = _clock.Today;
        var record = await _repo.Query().Include(a => a.Breaks)
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today, ct);
        if (record is null) return ApiResponse<long>.Fail("You haven't checked in today.");
        if (!string.IsNullOrEmpty(record.CheckOutTime)) return ApiResponse<long>.Fail("You've already checked out today.");
        if (record.Breaks.Any(b => string.IsNullOrEmpty(b.BreakInTime)))
            return ApiResponse<long>.Fail("You're already on a break. Use Break In to resume first.");

        var now = _clock.UtcNow.ToLocalTime();
        record.Breaks.Add(new AttendanceBreak { BreakOutTime = now.ToString("HH:mm"), SortOrder = record.Breaks.Count });
        _repo.Update(record);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(record.Id, "Break started.");
    }
}

/// <summary>End the open break (Break In) for the logged-in user's today record.</summary>
public sealed record BreakInCommand : IRequest<ApiResponse<long>>;

internal sealed class BreakInCommandHandler : IRequestHandler<BreakInCommand, ApiResponse<long>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public BreakInCommandHandler(IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IUnitOfWork uow, ICurrentUserService currentUser, IDateTimeProvider clock)
    { _repo = repo; _employeeRepo = employeeRepo; _uow = uow; _currentUser = currentUser; _clock = clock; }

    public async Task<ApiResponse<long>> Handle(BreakInCommand cmd, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null) return ApiResponse<long>.Fail("Your login isn't linked to an active employee.");

        var today = _clock.Today;
        var record = await _repo.Query().Include(a => a.Breaks)
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today, ct);
        if (record is null) return ApiResponse<long>.Fail("You haven't checked in today.");

        var open = record.Breaks.FirstOrDefault(b => string.IsNullOrEmpty(b.BreakInTime));
        if (open is null) return ApiResponse<long>.Fail("You're not currently on a break.");

        var now = _clock.UtcNow.ToLocalTime();
        open.BreakInTime = now.ToString("HH:mm");
        if (TimeOnly.TryParse(open.BreakOutTime, out var bo))
            open.Minutes = Math.Max(0, (int)(TimeOnly.FromDateTime(now.DateTime) - bo).TotalMinutes);

        _repo.Update(record);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(record.Id, "Break ended.");
    }
}
