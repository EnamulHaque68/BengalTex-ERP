using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Commands;

/// <summary>
/// Supervisor decision on a Pending check-in (selfie / geo / network flagged). Approve clears the
/// flag; Reject records a reason. Only the employee's direct supervisor or an org-wide reviewer may act.
/// </summary>
public sealed record DecideAttendanceApprovalCommand(long AttendanceId, bool Approve, string? RejectionReason)
    : IRequest<ApiResponse<AttendanceRecordDto>>;

public sealed class DecideAttendanceApprovalCommandValidator : AbstractValidator<DecideAttendanceApprovalCommand>
{
    public DecideAttendanceApprovalCommandValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().When(x => !x.Approve)
            .WithMessage("A reason is required when rejecting.");
        RuleFor(x => x.RejectionReason).MaximumLength(1000);
    }
}

internal sealed class DecideAttendanceApprovalCommandHandler
    : IRequestHandler<DecideAttendanceApprovalCommand, ApiResponse<AttendanceRecordDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public DecideAttendanceApprovalCommandHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IUnitOfWork uow, ICurrentUserService currentUser, IDateTimeProvider clock, IMediator mediator)
    { _repo = repo; _employeeRepo = employeeRepo; _uow = uow; _currentUser = currentUser; _clock = clock; _mediator = mediator; }

    public async Task<ApiResponse<AttendanceRecordDto>> Handle(DecideAttendanceApprovalCommand cmd, CancellationToken ct)
    {
        var reviewer = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (reviewer is null) return ApiResponse<AttendanceRecordDto>.Fail("Your login isn't linked to an active employee.");

        var record = await _repo.Query().Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == cmd.AttendanceId, ct);
        if (record is null) return ApiResponse<AttendanceRecordDto>.Fail("Attendance record not found.");

        var isDirectSupervisor = record.Employee.ReportingToEmployeeId == reviewer.Id;
        if (!isDirectSupervisor && !AttendanceSupervision.SeesAll(_currentUser))
            return ApiResponse<AttendanceRecordDto>.Fail("You can only review attendance for your own team members.");

        if (record.ApprovalStatus != AttendanceApprovalStatus.Pending)
            return ApiResponse<AttendanceRecordDto>.Fail($"This check-in is already {record.ApprovalStatus}.");

        record.ApprovalStatus = cmd.Approve ? AttendanceApprovalStatus.Approved : AttendanceApprovalStatus.Rejected;
        record.ApprovedByEmployeeId = reviewer.Id;
        record.ApprovedAt = _clock.UtcNow;
        record.RejectionReason = cmd.Approve ? null : cmd.RejectionReason?.Trim();

        _repo.Update(record);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetAttendanceByIdQuery(record.Id), ct);
    }
}
