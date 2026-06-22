using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Commands;

/// <summary>Upsert the (singleton) company attendance policy. Creates the row on first save.</summary>
public sealed record UpdateAttendanceSettingsCommand(
    string OfficeStartTime,
    string OfficeEndTime,
    int GracePeriodMinutes,
    string OutsideFenceMode,
    int DefaultRadiusMeters,
    bool RequireSelfie,
    bool RequireSupervisorApproval,
    bool AllowRemote,
    bool AllowFieldVisit) : IRequest<ApiResponse<AttendanceSettingsDto>>;

public sealed class UpdateAttendanceSettingsCommandValidator : AbstractValidator<UpdateAttendanceSettingsCommand>
{
    public UpdateAttendanceSettingsCommandValidator()
    {
        RuleFor(x => x.OfficeStartTime).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Office start time must be HH:mm.");
        RuleFor(x => x.OfficeEndTime).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Office end time must be HH:mm.");
        RuleFor(x => x.GracePeriodMinutes).InclusiveBetween(0, 240);
        RuleFor(x => x.DefaultRadiusMeters).InclusiveBetween(5, 100000);
        RuleFor(x => x.OutsideFenceMode).Must(m => Enum.TryParse<OutsideFenceMode>(m, out _)).WithMessage("Invalid fence mode.");
    }
}

internal sealed class UpdateAttendanceSettingsCommandHandler
    : IRequestHandler<UpdateAttendanceSettingsCommand, ApiResponse<AttendanceSettingsDto>>
{
    private readonly IRepository<AttendanceSettings> _repo;
    private readonly IRepository<Domain.Entities.Company> _companyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateAttendanceSettingsCommandHandler(
        IRepository<AttendanceSettings> repo, IRepository<Domain.Entities.Company> companyRepo, IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _companyRepo = companyRepo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<AttendanceSettingsDto>> Handle(UpdateAttendanceSettingsCommand cmd, CancellationToken ct)
    {
        var s = await _repo.Query().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (s is null)
        {
            var companyId = await _companyRepo.Query().AsNoTracking().OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync(ct);
            if (companyId == 0) return ApiResponse<AttendanceSettingsDto>.Fail("No company is configured. Create a company first.");
            s = new AttendanceSettings { CompanyId = companyId };
            await _repo.AddAsync(s, ct);
        }

        s.OfficeStartTime = TimeOnly.Parse(cmd.OfficeStartTime);
        s.OfficeEndTime = TimeOnly.Parse(cmd.OfficeEndTime);
        s.GracePeriodMinutes = cmd.GracePeriodMinutes;
        s.OutsideFenceMode = Enum.Parse<OutsideFenceMode>(cmd.OutsideFenceMode);
        s.DefaultRadiusMeters = cmd.DefaultRadiusMeters;
        s.RequireSelfie = cmd.RequireSelfie;
        s.RequireSupervisorApproval = cmd.RequireSupervisorApproval;
        s.AllowRemote = cmd.AllowRemote;
        s.AllowFieldVisit = cmd.AllowFieldVisit;

        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetAttendanceSettingsQuery(), ct);
    }
}
