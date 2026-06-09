using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

public sealed record GetAttendanceByIdQuery(long Id) : IRequest<ApiResponse<AttendanceRecordDto>>;

internal sealed class GetAttendanceByIdQueryHandler
    : IRequestHandler<GetAttendanceByIdQuery, ApiResponse<AttendanceRecordDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;

    public GetAttendanceByIdQueryHandler(IRepository<AttendanceRecord, long> repo) => _repo = repo;

    public async Task<ApiResponse<AttendanceRecordDto>> Handle(
        GetAttendanceByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _repo.Query()
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new AttendanceRecordDto(
                a.Id, a.EmployeeId, a.Employee.Code, a.Employee.FullName,
                a.AttendanceDate, a.Status.ToString(),
                a.CheckInTime, a.CheckOutTime, a.OvertimeHours, a.Notes,
                a.CheckInLatitude, a.CheckInLongitude,
                a.CheckInDistanceMeters, a.CheckInWithinFence))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ApiResponse<AttendanceRecordDto>.Fail("Attendance record not found.")
            : ApiResponse<AttendanceRecordDto>.Ok(dto);
    }
}
