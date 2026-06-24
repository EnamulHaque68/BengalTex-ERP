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
/// Self check-out for the logged-in user. Stamps check-out time + geo (+ optional selfie),
/// computes worked minutes (gross − breaks), and classifies early-leave / overtime against
/// the company office end-time.
/// </summary>
public sealed record SelfCheckOutCommand(
    double? Latitude, double? Longitude, string? SelfieBase64 = null)
    : IRequest<ApiResponse<AttendanceRecordDto>>;

public sealed class SelfCheckOutCommandValidator : AbstractValidator<SelfCheckOutCommand>
{
    public SelfCheckOutCommandValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x).Must(c => c.Latitude.HasValue == c.Longitude.HasValue)
            .WithMessage("Provide both Latitude and Longitude, or neither.");
    }
}

internal sealed class SelfCheckOutCommandHandler : IRequestHandler<SelfCheckOutCommand, ApiResponse<AttendanceRecordDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<AttendanceSettings> _settingsRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IGeoFenceService _geoFence;
    private readonly IFileStorage _files;
    private readonly IReverseGeocodeService _geocode;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public SelfCheckOutCommandHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IRepository<AttendanceSettings> settingsRepo, IUnitOfWork uow, ICurrentUserService currentUser,
        IGeoFenceService geoFence, IFileStorage files, IReverseGeocodeService geocode,
        IDateTimeProvider clock, IMediator mediator)
    {
        _repo = repo; _employeeRepo = employeeRepo; _settingsRepo = settingsRepo; _uow = uow;
        _currentUser = currentUser; _geoFence = geoFence; _files = files; _geocode = geocode; _clock = clock; _mediator = mediator;
    }

    public async Task<ApiResponse<AttendanceRecordDto>> Handle(SelfCheckOutCommand cmd, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null) return ApiResponse<AttendanceRecordDto>.Fail("Your login isn't linked to an active employee.");

        var today = _clock.Today;
        var record = await _repo.Query().Include(a => a.Breaks)
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today, ct);
        if (record is null) return ApiResponse<AttendanceRecordDto>.Fail("You haven't checked in today.");
        if (!string.IsNullOrEmpty(record.CheckOutTime)) return ApiResponse<AttendanceRecordDto>.Fail("You've already checked out today.");
        if (record.Breaks.Any(b => string.IsNullOrEmpty(b.BreakInTime)))
            return ApiResponse<AttendanceRecordDto>.Fail("End your current break (Break In) before checking out.");

        var policy = AttendancePolicyValues.From(await _settingsRepo.Query().FirstOrDefaultAsync(ct));
        var selfiePath = await AttendanceSelfie.SaveAsync(_files, cmd.SelfieBase64, $"checkout-{employee.Code}-{today:yyyyMMdd}", ct);

        // Geo capture (informational on check-out) + reverse-geocoded address (best-effort, mirrors check-in)
        double? distance = null; bool? insideFence = null; string? address = null;
        if (cmd.Latitude.HasValue && cmd.Longitude.HasValue)
        {
            var loc = GeoLocation.Create(cmd.Latitude.Value, cmd.Longitude.Value);
            var office = await _geoFence.ValidateForEmployeeAsync(employee.Id, loc, ct);
            if (office.HasAnyFence) { distance = office.NearestDistanceMeters; insideFence = office.IsInsideAnyFence; }
            try { address = await _geocode.ReverseAsync(cmd.Latitude.Value, cmd.Longitude.Value, ct); } catch { /* fail-safe */ }
        }

        var now = _clock.UtcNow.ToLocalTime();
        var checkOutTod = TimeOnly.FromDateTime(now.DateTime);
        var checkInTod = TimeOnly.TryParse(record.CheckInTime, out var ci) ? ci : checkOutTod;
        var breakMinutes = record.Breaks.Where(b => b.Minutes.HasValue).Sum(b => b.Minutes!.Value);
        var (worked, isEarlyLeave, overtime) = AttendancePolicy.ClassifyCheckOut(checkInTod, checkOutTod, breakMinutes, policy);

        record.CheckOutTime = now.ToString("HH:mm");
        record.CheckOutLatitude = cmd.Latitude;
        record.CheckOutLongitude = cmd.Longitude;
        record.CheckOutDistanceMeters = distance;
        record.CheckOutWithinFence = insideFence;
        record.CheckOutAddress = address;
        record.CheckOutSelfieUrl = selfiePath ?? record.CheckOutSelfieUrl;
        record.WorkedMinutes = worked;
        record.IsEarlyLeave = isEarlyLeave;
        record.OvertimeHours = overtime;

        _repo.Update(record);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetAttendanceByIdQuery(record.Id), ct);
    }
}
