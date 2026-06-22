using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Commands;

/// <summary>
/// Employee self-service: raise a request to add / correct a day's attendance. Goes to the
/// supervisor's inbox as Pending. RequestType + Reason are required; times are "HH:mm".
/// </summary>
public sealed record SubmitAttendanceRequestCommand(
    DateOnly RequestDate,
    string RequestType,
    string? RequestedCheckInTime,
    string? RequestedCheckOutTime,
    string? RequestedStatus,
    string Reason) : IRequest<ApiResponse<long>>;

public sealed class SubmitAttendanceRequestCommandValidator : AbstractValidator<SubmitAttendanceRequestCommand>
{
    private static readonly string[] TimeFormats = { "HH:mm", "H:mm" };

    public SubmitAttendanceRequestCommandValidator()
    {
        RuleFor(x => x.RequestType).NotEmpty()
            .Must(t => Enum.TryParse<AttendanceRequestType>(t, out _))
            .WithMessage("Invalid request type.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.RequestDate).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Request date cannot be in the future.");
        RuleFor(x => x.RequestedCheckInTime).Must(BeValidTime!).When(x => !string.IsNullOrWhiteSpace(x.RequestedCheckInTime))
            .WithMessage("Check-in time must be HH:mm.");
        RuleFor(x => x.RequestedCheckOutTime).Must(BeValidTime!).When(x => !string.IsNullOrWhiteSpace(x.RequestedCheckOutTime))
            .WithMessage("Check-out time must be HH:mm.");
        RuleFor(x => x.RequestedStatus).Must(s => Enum.TryParse<AttendanceStatus>(s, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.RequestedStatus))
            .WithMessage("Invalid requested status.");
    }

    private static bool BeValidTime(string t) => TimeOnly.TryParse(t, out _);
}

internal sealed class SubmitAttendanceRequestCommandHandler : IRequestHandler<SubmitAttendanceRequestCommand, ApiResponse<long>>
{
    private readonly IRepository<AttendanceRequest, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public SubmitAttendanceRequestCommandHandler(
        IRepository<AttendanceRequest, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IUnitOfWork uow, ICurrentUserService currentUser)
    { _repo = repo; _employeeRepo = employeeRepo; _uow = uow; _currentUser = currentUser; }

    public async Task<ApiResponse<long>> Handle(SubmitAttendanceRequestCommand cmd, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null) return ApiResponse<long>.Fail("Your login isn't linked to an active employee.");

        // Block a second open request for the same date
        var hasOpen = await _repo.Query().AnyAsync(
            r => r.EmployeeId == employee.Id && r.RequestDate == cmd.RequestDate
                 && r.Status == AttendanceRequestStatus.Pending, ct);
        if (hasOpen) return ApiResponse<long>.Fail($"You already have a pending request for {cmd.RequestDate:yyyy-MM-dd}.");

        var entity = new AttendanceRequest
        {
            EmployeeId = employee.Id,
            RequestDate = cmd.RequestDate,
            RequestType = Enum.Parse<AttendanceRequestType>(cmd.RequestType),
            RequestedCheckInTime = string.IsNullOrWhiteSpace(cmd.RequestedCheckInTime) ? null : cmd.RequestedCheckInTime.Trim(),
            RequestedCheckOutTime = string.IsNullOrWhiteSpace(cmd.RequestedCheckOutTime) ? null : cmd.RequestedCheckOutTime.Trim(),
            RequestedStatus = string.IsNullOrWhiteSpace(cmd.RequestedStatus) ? null : Enum.Parse<AttendanceStatus>(cmd.RequestedStatus),
            Reason = cmd.Reason.Trim(),
            Status = AttendanceRequestStatus.Pending
        };

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Request submitted for supervisor review.");
    }
}

/// <summary>Employee cancels their own still-pending request.</summary>
public sealed record CancelAttendanceRequestCommand(long RequestId) : IRequest<ApiResponse<long>>;

internal sealed class CancelAttendanceRequestCommandHandler : IRequestHandler<CancelAttendanceRequestCommand, ApiResponse<long>>
{
    private readonly IRepository<AttendanceRequest, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CancelAttendanceRequestCommandHandler(
        IRepository<AttendanceRequest, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IUnitOfWork uow, ICurrentUserService currentUser)
    { _repo = repo; _employeeRepo = employeeRepo; _uow = uow; _currentUser = currentUser; }

    public async Task<ApiResponse<long>> Handle(CancelAttendanceRequestCommand cmd, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null) return ApiResponse<long>.Fail("Your login isn't linked to an active employee.");

        var req = await _repo.Query().FirstOrDefaultAsync(r => r.Id == cmd.RequestId, ct);
        if (req is null) return ApiResponse<long>.Fail("Request not found.");
        if (req.EmployeeId != employee.Id) return ApiResponse<long>.Fail("You can only cancel your own requests.");
        if (req.Status != AttendanceRequestStatus.Pending) return ApiResponse<long>.Fail("Only pending requests can be cancelled.");

        req.Status = AttendanceRequestStatus.Cancelled;
        _repo.Update(req);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(req.Id, "Request cancelled.");
    }
}
