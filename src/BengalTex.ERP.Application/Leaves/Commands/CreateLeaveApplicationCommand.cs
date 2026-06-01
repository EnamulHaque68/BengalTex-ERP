using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Leaves.Commands;

/// <summary>
/// Submit a leave application. Server computes TotalDays from working days in the range
/// (excludes Friday weekend + active Holidays). Validates that the employee has enough
/// remaining balance for the year of FromDate.
/// </summary>
public sealed record CreateLeaveApplicationCommand(
    int EmployeeId,
    int LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reason,
    bool WriteAttendance,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateLeaveApplicationCommandValidator : AbstractValidator<CreateLeaveApplicationCommand>
{
    public CreateLeaveApplicationCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.LeaveTypeId).GreaterThan(0);
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.Reason).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateLeaveApplicationCommandHandler
    : IRequestHandler<CreateLeaveApplicationCommand, ApiResponse<long>>
{
    private readonly IRepository<LeaveApplication, long> _repo;
    private readonly IRepository<LeaveType> _typeRepo;
    private readonly IRepository<LeaveBalance> _balRepo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IRepository<Holiday> _holidayRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly INotificationService _notifications;

    public CreateLeaveApplicationCommandHandler(
        IRepository<LeaveApplication, long> repo,
        IRepository<LeaveType> typeRepo,
        IRepository<LeaveBalance> balRepo,
        IRepository<Domain.Entities.Employee> empRepo,
        IRepository<Holiday> holidayRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        INotificationService notifications)
    {
        _repo = repo; _typeRepo = typeRepo; _balRepo = balRepo;
        _empRepo = empRepo; _holidayRepo = holidayRepo;
        _uow = uow; _numbering = numbering; _notifications = notifications;
    }

    public async Task<ApiResponse<long>> Handle(CreateLeaveApplicationCommand cmd, CancellationToken ct)
    {
        var emp = await _empRepo.GetByIdAsync(cmd.EmployeeId, ct);
        if (emp is null || !emp.IsActive) return ApiResponse<long>.Fail("Employee not found or inactive.");

        var lt = await _typeRepo.GetByIdAsync(cmd.LeaveTypeId, ct);
        if (lt is null || !lt.IsActive) return ApiResponse<long>.Fail("Leave type not found or inactive.");

        // Overlap check — no overlap with existing Pending/Approved leave
        if (await _repo.Query().AnyAsync(a =>
            a.EmployeeId == cmd.EmployeeId
            && (a.Status == LeaveApplicationStatus.Pending || a.Status == LeaveApplicationStatus.Approved)
            && a.FromDate <= cmd.ToDate && a.ToDate >= cmd.FromDate, ct))
            return ApiResponse<long>.Fail("Dates overlap an existing pending or approved leave.");

        // Compute working days
        var holidays = (await _holidayRepo.Query()
            .Where(h => h.IsActive && h.Date >= cmd.FromDate && h.Date <= cmd.ToDate)
            .Select(h => h.Date).ToListAsync(ct)).ToHashSet();
        var workingDays = WorkingDayCalculator.CountWorkingDays(cmd.FromDate, cmd.ToDate, holidays);
        if (workingDays <= 0)
            return ApiResponse<long>.Fail("Selected range has no working days (all weekend / holiday).");

        if (lt.MaxConsecutiveDays.HasValue && workingDays > lt.MaxConsecutiveDays.Value)
            return ApiResponse<long>.Fail($"{lt.Name} allows at most {lt.MaxConsecutiveDays} consecutive day(s); this is {workingDays}.");

        // Balance check (paid types only)
        if (lt.IsPaid && lt.AnnualEntitlement > 0)
        {
            var year = cmd.FromDate.Year;
            var bal = await _balRepo.Query()
                .FirstOrDefaultAsync(b => b.EmployeeId == cmd.EmployeeId && b.LeaveTypeId == cmd.LeaveTypeId && b.Year == year, ct);
            var remaining = bal is null ? lt.AnnualEntitlement : (bal.Entitled - bal.Taken);
            if (remaining < workingDays)
                return ApiResponse<long>.Fail($"Insufficient {lt.Name} balance: remaining {remaining}, requested {workingDays}.");
        }

        var code = await _numbering.NextAsync("LV", null, ct);
        var app = new LeaveApplication
        {
            Code = code,
            EmployeeId = cmd.EmployeeId,
            LeaveTypeId = cmd.LeaveTypeId,
            FromDate = cmd.FromDate,
            ToDate = cmd.ToDate,
            TotalDays = workingDays,
            Reason = string.IsNullOrWhiteSpace(cmd.Reason) ? null : cmd.Reason.Trim(),
            Status = LeaveApplicationStatus.Pending,
            WriteAttendance = cmd.WriteAttendance,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(app, ct);

        // Notify HR Manager — they need to approve. Record-only InApp; caller (this handler) owns SaveChanges.
        await _notifications.NotifyAsync(
            NotificationChannels.InApp,
            recipient: "HRManager",
            subject: $"Leave application {code} pending approval",
            body: $"{emp.FullName} ({emp.Code}) submitted a {lt.Name} request for {workingDays} day(s) " +
                  $"from {cmd.FromDate:yyyy-MM-dd} to {cmd.ToDate:yyyy-MM-dd}.",
            relatedType: "LeaveApplication", relatedId: 0, ct: ct);

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(app.Id, "Leave application submitted.");
    }
}
