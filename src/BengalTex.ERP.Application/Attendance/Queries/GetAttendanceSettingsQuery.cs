using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

/// <summary>The company attendance policy (or sensible defaults when not yet configured).</summary>
public sealed record GetAttendanceSettingsQuery : IRequest<ApiResponse<AttendanceSettingsDto>>;

internal sealed class GetAttendanceSettingsQueryHandler : IRequestHandler<GetAttendanceSettingsQuery, ApiResponse<AttendanceSettingsDto>>
{
    private readonly IRepository<AttendanceSettings> _repo;

    public GetAttendanceSettingsQueryHandler(IRepository<AttendanceSettings> repo) => _repo = repo;

    public async Task<ApiResponse<AttendanceSettingsDto>> Handle(GetAttendanceSettingsQuery req, CancellationToken ct)
    {
        var s = await _repo.Query().AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        var dto = s is null
            ? new AttendanceSettingsDto(0, "09:00", "18:00", 15, nameof(OutsideFenceMode.Flag), 10, false, false, false, false)
            : new AttendanceSettingsDto(
                s.Id, s.OfficeStartTime.ToString("HH:mm"), s.OfficeEndTime.ToString("HH:mm"),
                s.GracePeriodMinutes, s.OutsideFenceMode.ToString(), s.DefaultRadiusMeters,
                s.RequireSelfie, s.RequireSupervisorApproval, s.AllowRemote, s.AllowFieldVisit);
        return ApiResponse<AttendanceSettingsDto>.Ok(dto);
    }
}
