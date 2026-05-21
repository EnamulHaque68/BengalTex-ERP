using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Attendance.Commands;

public sealed record CreateAttendanceCommand(
    int EmployeeId,
    DateOnly AttendanceDate,
    string Status,                 // Present | Absent | Late | HalfDay | Leave | Holiday
    string? CheckInTime,
    string? CheckOutTime,
    decimal OvertimeHours,
    string? Notes
) : IRequest<ApiResponse<AttendanceRecordDto>>;

public sealed class CreateAttendanceCommandValidator : AbstractValidator<CreateAttendanceCommand>
{
    public CreateAttendanceCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.AttendanceDate).NotEmpty();
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => Enum.TryParse<AttendanceStatus>(s, out _))
            .WithMessage("Status must be Present, Absent, Late, HalfDay, Leave, or Holiday.");
        RuleFor(x => x.CheckInTime).MaximumLength(10);
        RuleFor(x => x.CheckOutTime).MaximumLength(10);
        RuleFor(x => x.OvertimeHours).InclusiveBetween(0, 24);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateAttendanceCommandHandler
    : IRequestHandler<CreateAttendanceCommand, ApiResponse<AttendanceRecordDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CreateAttendanceCommandHandler(
        IRepository<AttendanceRecord, long> repo,
        IRepository<Domain.Entities.Employee> employeeRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<AttendanceRecordDto>> Handle(
        CreateAttendanceCommand cmd, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepo.GetByIdAsync(cmd.EmployeeId, cancellationToken);
        if (employee is null) return ApiResponse<AttendanceRecordDto>.Fail("Employee not found.");

        if (await _repo.AnyAsync(a => a.EmployeeId == cmd.EmployeeId && a.AttendanceDate == cmd.AttendanceDate, cancellationToken))
            return ApiResponse<AttendanceRecordDto>.Fail("Attendance already recorded for this employee on this date — edit it instead.");

        var entity = new AttendanceRecord
        {
            EmployeeId = cmd.EmployeeId,
            AttendanceDate = cmd.AttendanceDate,
            Status = Enum.Parse<AttendanceStatus>(cmd.Status),
            CheckInTime = string.IsNullOrWhiteSpace(cmd.CheckInTime) ? null : cmd.CheckInTime.Trim(),
            CheckOutTime = string.IsNullOrWhiteSpace(cmd.CheckOutTime) ? null : cmd.CheckOutTime.Trim(),
            OvertimeHours = cmd.OvertimeHours,
            Notes = cmd.Notes
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetAttendanceByIdQuery(entity.Id), cancellationToken);
    }
}
