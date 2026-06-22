using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Commands;

/// <summary>
/// Supervisor decision on an employee's attendance correction request. On Approve the requested
/// times / status are applied to that employee's attendance row for the date (created if missing);
/// on Reject a note is recorded. Only the employee's direct supervisor or an org-wide reviewer may act.
/// </summary>
public sealed record DecideAttendanceRequestCommand(long RequestId, bool Approve, string? ReviewNote)
    : IRequest<ApiResponse<long>>;

public sealed class DecideAttendanceRequestCommandValidator : AbstractValidator<DecideAttendanceRequestCommand>
{
    public DecideAttendanceRequestCommandValidator()
    {
        RuleFor(x => x.ReviewNote).NotEmpty().When(x => !x.Approve)
            .WithMessage("A note is required when rejecting a request.");
        RuleFor(x => x.ReviewNote).MaximumLength(1000);
    }
}

internal sealed class DecideAttendanceRequestCommandHandler : IRequestHandler<DecideAttendanceRequestCommand, ApiResponse<long>>
{
    private readonly IRepository<AttendanceRequest, long> _requestRepo;
    private readonly IRepository<AttendanceRecord, long> _recordRepo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public DecideAttendanceRequestCommandHandler(
        IRepository<AttendanceRequest, long> requestRepo, IRepository<AttendanceRecord, long> recordRepo,
        IRepository<Domain.Entities.Employee> employeeRepo, IUnitOfWork uow,
        ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _requestRepo = requestRepo; _recordRepo = recordRepo; _employeeRepo = employeeRepo;
        _uow = uow; _currentUser = currentUser; _clock = clock;
    }

    public async Task<ApiResponse<long>> Handle(DecideAttendanceRequestCommand cmd, CancellationToken ct)
    {
        var reviewer = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (reviewer is null) return ApiResponse<long>.Fail("Your login isn't linked to an active employee.");

        var req = await _requestRepo.Query().Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == cmd.RequestId, ct);
        if (req is null) return ApiResponse<long>.Fail("Request not found.");
        if (req.Status != AttendanceRequestStatus.Pending)
            return ApiResponse<long>.Fail($"This request is already {req.Status}.");

        var isDirectSupervisor = req.Employee.ReportingToEmployeeId == reviewer.Id;
        if (!isDirectSupervisor && !AttendanceSupervision.SeesAll(_currentUser))
            return ApiResponse<long>.Fail("You can only review requests from your own team members.");

        req.ReviewedByEmployeeId = reviewer.Id;
        req.ReviewedAt = _clock.UtcNow;
        req.ReviewNote = cmd.ReviewNote?.Trim();

        if (!cmd.Approve)
        {
            req.Status = AttendanceRequestStatus.Rejected;
            _requestRepo.Update(req);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<long>.Ok(req.Id, "Request rejected.");
        }

        // ── Approve: apply to the attendance row (create if missing) ──
        var record = await _recordRepo.Query()
            .FirstOrDefaultAsync(a => a.EmployeeId == req.EmployeeId && a.AttendanceDate == req.RequestDate, ct);

        if (record is null)
        {
            record = new AttendanceRecord
            {
                EmployeeId = req.EmployeeId,
                AttendanceDate = req.RequestDate,
                Status = req.RequestedStatus ?? AttendanceStatus.Present,
                Mode = AttendanceMode.Office,
                CheckInTime = req.RequestedCheckInTime,
                CheckOutTime = req.RequestedCheckOutTime,
                Notes = $"Regularized via request #{req.Id}",
                ApprovalStatus = AttendanceApprovalStatus.Approved,
                ApprovedByEmployeeId = reviewer.Id,
                ApprovedAt = _clock.UtcNow
            };
            ApplyTimes(record);
            await _recordRepo.AddAsync(record, ct);
            await _uow.SaveChangesAsync(ct);   // generate the record Id before linking
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(req.RequestedCheckInTime)) record.CheckInTime = req.RequestedCheckInTime;
            if (!string.IsNullOrWhiteSpace(req.RequestedCheckOutTime)) record.CheckOutTime = req.RequestedCheckOutTime;
            if (req.RequestedStatus.HasValue) record.Status = req.RequestedStatus.Value;
            record.Notes = string.IsNullOrWhiteSpace(record.Notes)
                ? $"Corrected via request #{req.Id}"
                : $"{record.Notes} | Corrected via request #{req.Id}";
            ApplyTimes(record);
            _recordRepo.Update(record);
        }

        req.Status = AttendanceRequestStatus.Approved;
        req.AppliedAttendanceRecordId = record.Id;
        _requestRepo.Update(req);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(record.Id, "Request approved and attendance updated.");
    }

    /// <summary>Recompute worked minutes when both ends are present (no break deduction on manual corrections).</summary>
    private static void ApplyTimes(AttendanceRecord record)
    {
        if (TimeOnly.TryParse(record.CheckInTime, out var ci) && TimeOnly.TryParse(record.CheckOutTime, out var co))
            record.WorkedMinutes = Math.Max(0, (int)(co - ci).TotalMinutes);
    }
}
