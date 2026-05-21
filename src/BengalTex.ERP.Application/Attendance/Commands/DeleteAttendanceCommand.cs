using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Attendance.Commands;

public sealed record DeleteAttendanceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteAttendanceCommandHandler
    : IRequestHandler<DeleteAttendanceCommand, ApiResponse>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteAttendanceCommandHandler(IRepository<AttendanceRecord, long> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteAttendanceCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Attendance record not found.");

        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Attendance record deleted.");
    }
}
