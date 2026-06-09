using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Domain.ValueObjects;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Commands;

/// <summary>
/// Self-service attendance check-in for the currently-logged-in user, with optional GPS
/// coordinates. Behaviour:
///   • EmployeeId resolution: matches <c>Employee.Code == ICurrentUserService.UserName</c>
///     (the typical factory-floor convention where the login is the employee code).
///   • If GPS coords are supplied AND the user's FactoryId is known AND the factory has a
///     configured geo-fence, the <see cref="IGeoFenceService"/> validates the location and
///     stamps <c>CheckInLatitude / CheckInLongitude / CheckInDistanceMeters / CheckInWithinFence</c>.
///   • Per business rule (see GeoFenceService), check-in is NEVER blocked by being outside
///     the fence — it's flagged for review. Admins can see <c>CheckInWithinFence = false</c>
///     rows in the attendance list and decide whether to question them.
///   • If today's record already exists, returns a friendly error (re-checkin via Update).
/// </summary>
public sealed record SelfCheckInCommand(
    double? Latitude,
    double? Longitude,
    string? Notes
) : IRequest<ApiResponse<AttendanceRecordDto>>;

public sealed class SelfCheckInCommandValidator : AbstractValidator<SelfCheckInCommand>
{
    public SelfCheckInCommandValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x).Must(c => c.Latitude.HasValue == c.Longitude.HasValue)
            .WithMessage("Provide both Latitude and Longitude, or neither.");
    }
}

internal sealed class SelfCheckInCommandHandler
    : IRequestHandler<SelfCheckInCommand, ApiResponse<AttendanceRecordDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IGeoFenceService _geoFence;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public SelfCheckInCommandHandler(
        IRepository<AttendanceRecord, long> repo,
        IRepository<Domain.Entities.Employee> employeeRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IGeoFenceService geoFence,
        IDateTimeProvider clock,
        IMediator mediator)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _uow = uow;
        _currentUser = currentUser;
        _geoFence = geoFence;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<ApiResponse<AttendanceRecordDto>> Handle(SelfCheckInCommand cmd, CancellationToken ct)
    {
        var userName = _currentUser.UserName;
        if (string.IsNullOrWhiteSpace(userName))
            return ApiResponse<AttendanceRecordDto>.Fail("Not authenticated.");

        var employee = await _employeeRepo.Query()
            .Where(e => e.IsActive && e.Status == EmployeeStatus.Active && e.Code == userName)
            .FirstOrDefaultAsync(ct);
        if (employee is null)
            return ApiResponse<AttendanceRecordDto>.Fail(
                $"Could not match your login '{userName}' to an active employee record (by code). " +
                "Ask HR to set your Employee Code to match your login, or check in manually via the admin UI.");

        var today = _clock.Today;
        if (await _repo.AnyAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today, ct))
            return ApiResponse<AttendanceRecordDto>.Fail(
                $"You're already checked in for {today:yyyy-MM-dd}. To update OT or check-out time, go to Attendance.");

        // Geo-fence validation (best-effort — never blocks)
        double? distanceMeters = null;
        bool? insideFence = null;
        if (cmd.Latitude.HasValue && cmd.Longitude.HasValue && _currentUser.FactoryId.HasValue)
        {
            try
            {
                var location = GeoLocation.Create(cmd.Latitude.Value, cmd.Longitude.Value);
                var result = await _geoFence.ValidateAsync(_currentUser.FactoryId.Value, location, ct);
                // AllowedRadiusMeters = 0 sentinel means factory had no fence configured
                if (result.AllowedRadiusMeters > 0)
                {
                    distanceMeters = result.DistanceMeters;
                    insideFence = result.IsInsideFence;
                }
            }
            catch
            {
                // Don't block check-in over geo-fence lookup failures (factory not found, etc.)
                distanceMeters = null;
                insideFence = null;
            }
        }

        var now = _clock.UtcNow.ToLocalTime();
        var entity = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            AttendanceDate = today,
            Status = AttendanceStatus.Present,
            CheckInTime = now.ToString("HH:mm"),
            OvertimeHours = 0m,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            CheckInLatitude = cmd.Latitude,
            CheckInLongitude = cmd.Longitude,
            CheckInDistanceMeters = distanceMeters,
            CheckInWithinFence = insideFence
        };

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetAttendanceByIdQuery(entity.Id), ct);
    }
}
