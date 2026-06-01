using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Leaves.Commands;

// ── Approve ──
public sealed record ApproveLeaveApplicationCommand(long Id) : IRequest<ApiResponse>;

internal sealed class ApproveLeaveApplicationCommandHandler : IRequestHandler<ApproveLeaveApplicationCommand, ApiResponse>
{
    private readonly IRepository<LeaveApplication, long> _repo;
    private readonly IRepository<LeaveBalance> _balRepo;
    private readonly IRepository<LeaveType> _typeRepo;
    private readonly IRepository<AttendanceRecord, long> _attRepo;
    private readonly IRepository<Holiday> _holidayRepo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifications;

    public ApproveLeaveApplicationCommandHandler(
        IRepository<LeaveApplication, long> repo,
        IRepository<LeaveBalance> balRepo,
        IRepository<LeaveType> typeRepo,
        IRepository<AttendanceRecord, long> attRepo,
        IRepository<Holiday> holidayRepo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        INotificationService notifications)
    {
        _repo = repo; _balRepo = balRepo; _typeRepo = typeRepo;
        _attRepo = attRepo; _holidayRepo = holidayRepo; _empRepo = empRepo;
        _uow = uow; _currentUser = currentUser; _clock = clock; _notifications = notifications;
    }

    public async Task<ApiResponse> Handle(ApproveLeaveApplicationCommand cmd, CancellationToken ct)
    {
        var app = await _repo.GetByIdAsync(cmd.Id, ct);
        if (app is null) return ApiResponse.Fail("Leave application not found.");
        if (app.Status != LeaveApplicationStatus.Pending)
            return ApiResponse.Fail($"Cannot approve a {app.Status} application.");

        var lt = await _typeRepo.GetByIdAsync(app.LeaveTypeId, ct);
        if (lt is null) return ApiResponse.Fail("Leave type missing.");

        // Re-check + deduct balance (paid types only)
        if (lt.IsPaid && lt.AnnualEntitlement > 0)
        {
            var year = app.FromDate.Year;
            var bal = await _balRepo.Query()
                .FirstOrDefaultAsync(b => b.EmployeeId == app.EmployeeId && b.LeaveTypeId == app.LeaveTypeId && b.Year == year, ct);
            if (bal is null)
            {
                bal = new LeaveBalance { EmployeeId = app.EmployeeId, LeaveTypeId = app.LeaveTypeId, Year = year, Entitled = lt.AnnualEntitlement, Taken = 0 };
                await _balRepo.AddAsync(bal, ct);
            }
            var remaining = bal.Entitled - bal.Taken;
            if (remaining < app.TotalDays)
                return ApiResponse.Fail($"Insufficient balance at approval: remaining {remaining}, requested {app.TotalDays}.");
            bal.Taken += app.TotalDays;
            _balRepo.Update(bal);
        }

        // Optional: write AttendanceRecord rows
        if (app.WriteAttendance)
        {
            var holidays = (await _holidayRepo.Query()
                .Where(h => h.IsActive && h.Date >= app.FromDate && h.Date <= app.ToDate)
                .Select(h => h.Date).ToListAsync(ct)).ToHashSet();
            var existing = (await _attRepo.Query()
                .Where(a => a.EmployeeId == app.EmployeeId && a.AttendanceDate >= app.FromDate && a.AttendanceDate <= app.ToDate)
                .Select(a => a.AttendanceDate).ToListAsync(ct)).ToHashSet();
            foreach (var d in WorkingDayCalculator.EnumerateWorkingDays(app.FromDate, app.ToDate, holidays))
            {
                if (existing.Contains(d)) continue;     // do not overwrite manual entries
                await _attRepo.AddAsync(new AttendanceRecord
                {
                    EmployeeId = app.EmployeeId,
                    AttendanceDate = d,
                    Status = AttendanceStatus.Leave,
                    Notes = $"Auto from leave {app.Code}"
                }, ct);
            }
        }

        app.Status = LeaveApplicationStatus.Approved;
        app.DecidedAt = _clock.UtcNow;
        app.DecidedBy = _currentUser.UserName ?? _currentUser.UserId;
        _repo.Update(app);

        // Notify the employee — addressed by name (per-user filtering is v1c)
        var emp = await _empRepo.GetByIdAsync(app.EmployeeId, ct);
        if (emp is not null)
        {
            await _notifications.NotifyAsync(
                NotificationChannels.InApp,
                recipient: emp.FullName,
                subject: $"Leave {app.Code} approved",
                body: $"Your leave request from {app.FromDate:yyyy-MM-dd} to {app.ToDate:yyyy-MM-dd} " +
                      $"({app.TotalDays} day(s)) has been approved by {app.DecidedBy}.",
                relatedType: "LeaveApplication", relatedId: app.Id, ct: ct);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Leave {app.Code} approved.");
    }
}

// ── Reject ──
public sealed record RejectLeaveApplicationCommand(long Id, string? RejectionReason) : IRequest<ApiResponse>;

public sealed class RejectLeaveApplicationCommandValidator : AbstractValidator<RejectLeaveApplicationCommand>
{
    public RejectLeaveApplicationCommandValidator() { RuleFor(x => x.RejectionReason).MaximumLength(500); }
}

internal sealed class RejectLeaveApplicationCommandHandler : IRequestHandler<RejectLeaveApplicationCommand, ApiResponse>
{
    private readonly IRepository<LeaveApplication, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifications;

    public RejectLeaveApplicationCommandHandler(IRepository<LeaveApplication, long> repo,
        IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow,
        ICurrentUserService currentUser, IDateTimeProvider clock, INotificationService notifications)
    {
        _repo = repo; _empRepo = empRepo; _uow = uow;
        _currentUser = currentUser; _clock = clock; _notifications = notifications;
    }

    public async Task<ApiResponse> Handle(RejectLeaveApplicationCommand cmd, CancellationToken ct)
    {
        var app = await _repo.GetByIdAsync(cmd.Id, ct);
        if (app is null) return ApiResponse.Fail("Leave application not found.");
        if (app.Status != LeaveApplicationStatus.Pending)
            return ApiResponse.Fail($"Cannot reject a {app.Status} application.");
        app.Status = LeaveApplicationStatus.Rejected;
        app.RejectionReason = string.IsNullOrWhiteSpace(cmd.RejectionReason) ? null : cmd.RejectionReason.Trim();
        app.DecidedAt = _clock.UtcNow;
        app.DecidedBy = _currentUser.UserName ?? _currentUser.UserId;
        _repo.Update(app);

        var emp = await _empRepo.GetByIdAsync(app.EmployeeId, ct);
        if (emp is not null)
        {
            var reasonSuffix = string.IsNullOrEmpty(app.RejectionReason) ? "" : $" Reason: {app.RejectionReason}";
            await _notifications.NotifyAsync(
                NotificationChannels.InApp,
                recipient: emp.FullName,
                subject: $"Leave {app.Code} rejected",
                body: $"Your leave request from {app.FromDate:yyyy-MM-dd} to {app.ToDate:yyyy-MM-dd} " +
                      $"was rejected by {app.DecidedBy}.{reasonSuffix}",
                relatedType: "LeaveApplication", relatedId: app.Id, ct: ct);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Leave {app.Code} rejected.");
    }
}

// ── Cancel ── (own pending OR HR cancels approved → reverses balance + removes auto attendance rows)
public sealed record CancelLeaveApplicationCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CancelLeaveApplicationCommandHandler : IRequestHandler<CancelLeaveApplicationCommand, ApiResponse>
{
    private readonly IRepository<LeaveApplication, long> _repo;
    private readonly IRepository<LeaveBalance> _balRepo;
    private readonly IRepository<AttendanceRecord, long> _attRepo;
    private readonly IUnitOfWork _uow;

    public CancelLeaveApplicationCommandHandler(
        IRepository<LeaveApplication, long> repo, IRepository<LeaveBalance> balRepo,
        IRepository<AttendanceRecord, long> attRepo, IUnitOfWork uow)
    {
        _repo = repo; _balRepo = balRepo; _attRepo = attRepo; _uow = uow;
    }

    public async Task<ApiResponse> Handle(CancelLeaveApplicationCommand cmd, CancellationToken ct)
    {
        var app = await _repo.GetByIdAsync(cmd.Id, ct);
        if (app is null) return ApiResponse.Fail("Leave application not found.");
        if (app.Status == LeaveApplicationStatus.Cancelled || app.Status == LeaveApplicationStatus.Rejected)
            return ApiResponse.Fail($"Cannot cancel a {app.Status} application.");

        // Reverse balance if was Approved
        if (app.Status == LeaveApplicationStatus.Approved)
        {
            var year = app.FromDate.Year;
            var bal = await _balRepo.Query()
                .FirstOrDefaultAsync(b => b.EmployeeId == app.EmployeeId && b.LeaveTypeId == app.LeaveTypeId && b.Year == year, ct);
            if (bal is not null)
            {
                bal.Taken = Math.Max(0, bal.Taken - app.TotalDays);
                _balRepo.Update(bal);
            }

            // Remove auto-written attendance rows (only those tagged via Notes — preserve manual ones)
            if (app.WriteAttendance)
            {
                var marker = $"Auto from leave {app.Code}";
                var autoRows = await _attRepo.Query()
                    .Where(a => a.EmployeeId == app.EmployeeId
                                && a.AttendanceDate >= app.FromDate
                                && a.AttendanceDate <= app.ToDate
                                && a.Notes == marker)
                    .ToListAsync(ct);
                foreach (var r in autoRows) _attRepo.Remove(r);
            }
        }

        app.Status = LeaveApplicationStatus.Cancelled;
        _repo.Update(app);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Leave {app.Code} cancelled.");
    }
}
