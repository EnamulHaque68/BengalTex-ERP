using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Attendance.Commands;

public sealed record UpdateAttendanceCommand(
    long Id,
    string Status,
    string? CheckInTime,
    string? CheckOutTime,
    decimal OvertimeHours,
    string? Notes
) : IRequest<ApiResponse<AttendanceRecordDto>>;

public sealed class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => Enum.TryParse<AttendanceStatus>(s, out _))
            .WithMessage("Status must be Present, Absent, Late, HalfDay, Leave, or Holiday.");
        RuleFor(x => x.CheckInTime).MaximumLength(10);
        RuleFor(x => x.CheckOutTime).MaximumLength(10);
        RuleFor(x => x.OvertimeHours).InclusiveBetween(0, 24);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateAttendanceCommandHandler
    : IRequestHandler<UpdateAttendanceCommand, ApiResponse<AttendanceRecordDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateAttendanceCommandHandler(
        IRepository<AttendanceRecord, long> repo, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<AttendanceRecordDto>> Handle(
        UpdateAttendanceCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<AttendanceRecordDto>.Fail("Attendance record not found.");

        entity.Status = Enum.Parse<AttendanceStatus>(cmd.Status);
        entity.CheckInTime = string.IsNullOrWhiteSpace(cmd.CheckInTime) ? null : cmd.CheckInTime.Trim();
        entity.CheckOutTime = string.IsNullOrWhiteSpace(cmd.CheckOutTime) ? null : cmd.CheckOutTime.Trim();
        entity.OvertimeHours = cmd.OvertimeHours;
        entity.Notes = cmd.Notes;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetAttendanceByIdQuery(entity.Id), cancellationToken);
    }
}
