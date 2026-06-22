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

/// <summary>Shared helper: decode a base64 (data-URL) selfie and store it via IFileStorage.</summary>
internal static class AttendanceSelfie
{
    public static async Task<string?> SaveAsync(IFileStorage files, string? base64, string label, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        var comma = base64.IndexOf(',');
        var raw = comma >= 0 ? base64[(comma + 1)..] : base64;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(raw); } catch { return null; }
        if (bytes.Length == 0) return null;
        using var ms = new MemoryStream(bytes);
        var res = await files.SaveAsync(ms, $"{label}.jpg", "image/jpeg", "Attendance", ct);
        return res.StoragePath;
    }
}

/// <summary>Resolves the current user's employee record — by login link (UserId) first, then by code.</summary>
internal static class AttendanceResolver
{
    public static async Task<Domain.Entities.Employee?> ResolveAsync(
        IRepository<Domain.Entities.Employee> repo, ICurrentUserService user, CancellationToken ct)
    {
        var uid = user.UserId;
        if (!string.IsNullOrEmpty(uid))
        {
            var byUser = await repo.Query().FirstOrDefaultAsync(
                e => e.IsActive && e.Status == EmployeeStatus.Active && e.UserId == uid, ct);
            if (byUser is not null) return byUser;
        }
        var name = user.UserName;
        if (string.IsNullOrEmpty(name)) return null;
        return await repo.Query().FirstOrDefaultAsync(
            e => e.IsActive && e.Status == EmployeeStatus.Active && e.Code == name, ct);
    }
}

/// <summary>
/// Self check-in for the logged-in user. Resolves the employee by login link (Employee.UserId),
/// validates against the employee's authorized office locations (multi-location geo-fence),
/// classifies On-Time/Late against the company office hours + grace, optionally captures a selfie,
/// and routes flagged/selfie/approval-required check-ins to Pending supervisor approval.
/// Per policy, an outside-fence check-in is FLAGGED (accepted) unless OutsideFenceMode = Block.
/// </summary>
public sealed record SelfCheckInCommand(
    double? Latitude, double? Longitude, string? Notes, string? SelfieBase64 = null)
    : IRequest<ApiResponse<AttendanceRecordDto>>;

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

internal sealed class SelfCheckInCommandHandler : IRequestHandler<SelfCheckInCommand, ApiResponse<AttendanceRecordDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<AttendanceSettings> _settingsRepo;
    private readonly IRepository<Holiday> _holidayRepo;
    private readonly IRepository<Shift> _shiftRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IGeoFenceService _geoFence;
    private readonly IFileStorage _files;
    private readonly IFaceMatchService _faceMatch;
    private readonly IReverseGeocodeService _geocode;
    private readonly INetworkIntelligenceService _network;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public SelfCheckInCommandHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IRepository<AttendanceSettings> settingsRepo, IRepository<Holiday> holidayRepo, IRepository<Shift> shiftRepo,
        IUnitOfWork uow, ICurrentUserService currentUser,
        IGeoFenceService geoFence, IFileStorage files, IFaceMatchService faceMatch,
        IReverseGeocodeService geocode, INetworkIntelligenceService network,
        IDateTimeProvider clock, IMediator mediator)
    {
        _repo = repo; _employeeRepo = employeeRepo; _settingsRepo = settingsRepo;
        _holidayRepo = holidayRepo; _shiftRepo = shiftRepo; _uow = uow;
        _currentUser = currentUser; _geoFence = geoFence; _files = files; _faceMatch = faceMatch;
        _geocode = geocode; _network = network; _clock = clock; _mediator = mediator;
    }

    public async Task<ApiResponse<AttendanceRecordDto>> Handle(SelfCheckInCommand cmd, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null)
            return ApiResponse<AttendanceRecordDto>.Fail(
                "Your login isn't linked to an active employee. Ask HR to link your account (Employee → Edit Profile → Login Account).");

        var today = _clock.Today;
        if (await _repo.AnyAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today, ct))
            return ApiResponse<AttendanceRecordDto>.Fail($"You're already checked in for {today:yyyy-MM-dd}.");

        var policy = AttendancePolicyValues.From(await _settingsRepo.Query().FirstOrDefaultAsync(ct));

        // Selfie (anti buddy-punch)
        if (policy.RequireSelfie && string.IsNullOrWhiteSpace(cmd.SelfieBase64))
            return ApiResponse<AttendanceRecordDto>.Fail("A live selfie is required to check in. Please allow camera access.");
        var selfiePath = await AttendanceSelfie.SaveAsync(_files, cmd.SelfieBase64, $"checkin-{employee.Code}-{today:yyyyMMdd}", ct);

        // Geo-fence — multi-location first, legacy factory fence as fallback
        double? distance = null; bool? insideFence = null; int? matchedLocationId = null;
        if (cmd.Latitude.HasValue && cmd.Longitude.HasValue)
        {
            var loc = GeoLocation.Create(cmd.Latitude.Value, cmd.Longitude.Value);
            var office = await _geoFence.ValidateForEmployeeAsync(employee.Id, loc, ct);
            if (office.HasAnyFence)
            {
                distance = office.NearestDistanceMeters;
                insideFence = office.IsInsideAnyFence;
                matchedLocationId = office.MatchedOfficeLocationId;
            }
            else if (_currentUser.FactoryId.HasValue)
            {
                try
                {
                    var legacy = await _geoFence.ValidateAsync(_currentUser.FactoryId.Value, loc, ct);
                    if (legacy.AllowedRadiusMeters > 0) { distance = legacy.DistanceMeters; insideFence = legacy.IsInsideFence; }
                }
                catch { /* never block over fence lookup failures */ }
            }
        }

        if (insideFence == false && policy.OutsideFenceMode == OutsideFenceMode.Block)
            return ApiResponse<AttendanceRecordDto>.Fail(
                $"You're outside the allowed office area ({distance:0}m away). Check-in is blocked by company policy.");

        var now = _clock.UtcNow.ToLocalTime();
        var checkInTod = TimeOnly.FromDateTime(now.DateTime);
        var (status, isLate) = AttendancePolicy.ClassifyCheckIn(checkInTod, policy);

        // ── Off-day / holiday work: checking in on a holiday or the employee's weekend
        //    re-categorises the day (these still count as present + are eligible for OT) ──
        var isHoliday = await _holidayRepo.Query().AsNoTracking()
            .AnyAsync(h => h.IsActive && h.Date == today, ct);
        var isOffday = false;
        if (!isHoliday && employee.ShiftId.HasValue)
        {
            var shift = await _shiftRepo.Query().AsNoTracking().FirstOrDefaultAsync(s => s.Id == employee.ShiftId.Value, ct);
            if (shift is not null)
                isOffday = today.DayOfWeek == shift.WeekendDayOfWeek
                    || (shift.SecondWeekendDayOfWeek.HasValue && today.DayOfWeek == shift.SecondWeekendDayOfWeek.Value);
        }
        if (isHoliday) status = AttendanceStatus.HolidayWork;
        else if (isOffday) status = AttendanceStatus.OffdayWork;

        // ── Location & network intelligence (best-effort, never blocks check-in) ──
        var device = UserAgentParser.Parse(_currentUser.UserAgent);
        var ip = _currentUser.IpAddress;
        var geocodeTask = cmd.Latitude.HasValue && cmd.Longitude.HasValue
            ? _geocode.ReverseAsync(cmd.Latitude.Value, cmd.Longitude.Value, ct)
            : Task.FromResult<string?>(null);
        var networkTask = _network.InspectAsync(ip, ct);
        string? address = null; NetworkInspection network = new(null, null, null);
        try { address = await geocodeTask; } catch { /* fail-safe */ }
        try { network = await networkTask; } catch { /* fail-safe */ }

        // Approval needed when policy requires it, a selfie was taken (review identity), flagged outside, or VPN/proxy
        var needsReview = policy.RequireSupervisorApproval || policy.RequireSelfie
            || insideFence == false || network.IsProxyVpn == true;
        var faceOutcome = selfiePath is null ? null : await _faceMatch.CompareAsync(selfiePath, employee.PhotoUrl, ct);

        var entity = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            AttendanceDate = today,
            Status = status,
            IsLate = isLate && !isHoliday && !isOffday,
            IsHolidayWork = isHoliday,
            IsOffdayWork = isOffday,
            Mode = AttendanceMode.Office,
            CheckInTime = now.ToString("HH:mm"),
            OvertimeHours = 0m,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            CheckInLatitude = cmd.Latitude,
            CheckInLongitude = cmd.Longitude,
            CheckInDistanceMeters = distance,
            CheckInWithinFence = insideFence,
            MatchedOfficeLocationId = matchedLocationId,
            CheckInSelfieUrl = selfiePath,
            FaceMatchScore = faceOutcome?.Score,
            FaceMatchStatus = Enum.TryParse<FaceMatchStatus>(faceOutcome?.Status, out var fm) ? fm : FaceMatchStatus.NotChecked,
            ApprovalStatus = needsReview ? AttendanceApprovalStatus.Pending : AttendanceApprovalStatus.NotRequired,
            CheckInAddress = address,
            CheckInIpAddress = ip,
            CheckInDeviceType = device.DeviceType,
            CheckInBrowser = device.Browser,
            CheckInOs = device.Os,
            CheckInIsProxyVpn = network.IsProxyVpn,
            CheckInIsp = network.Isp,
            CheckInNetworkNote = network.Note
        };

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetAttendanceByIdQuery(entity.Id), ct);
    }
}
