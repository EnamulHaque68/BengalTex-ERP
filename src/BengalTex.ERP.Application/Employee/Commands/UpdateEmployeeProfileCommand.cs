using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Employee.Commands;

/// <summary>
/// Updates the HR-profile detail fields of an employee (the "Edit Profile" flow) — separate from the
/// core employee CRUD so the existing create/update surface stays untouched. UserId links a login
/// account for self-service profile access.
/// </summary>
public sealed record UpdateEmployeeProfileCommand(
    int EmployeeId,
    string? PhotoUrl,
    string? BloodGroup,
    string MaritalStatus,
    string? Religion,
    string? Nationality,
    string? WorkLocation,
    string? AboutMe,
    DateOnly? ProbationEndDate,
    DateOnly? ConfirmationDate,
    int? ReportingToEmployeeId,
    string? UserId) : IRequest<ApiResponse<int>>;

public sealed class UpdateEmployeeProfileCommandValidator : AbstractValidator<UpdateEmployeeProfileCommand>
{
    public UpdateEmployeeProfileCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.BloodGroup).MaximumLength(10);
        RuleFor(x => x.MaritalStatus).Must(s => Enum.TryParse<MaritalStatus>(s, out _)).WithMessage("Invalid marital status.");
        RuleFor(x => x.Religion).MaximumLength(50);
        RuleFor(x => x.Nationality).MaximumLength(100);
        RuleFor(x => x.WorkLocation).MaximumLength(150);
        RuleFor(x => x.AboutMe).MaximumLength(1000);
        RuleFor(x => x.UserId).MaximumLength(450);
        RuleFor(x => x).Must(x => x.ReportingToEmployeeId != x.EmployeeId)
            .WithMessage("An employee cannot report to themselves.");
    }
}

internal sealed class UpdateEmployeeProfileCommandHandler : IRequestHandler<UpdateEmployeeProfileCommand, ApiResponse<int>>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateEmployeeProfileCommandHandler(IRepository<Domain.Entities.Employee> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateEmployeeProfileCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.EmployeeId, ct);
        if (e is null) return ApiResponse<int>.Fail("Employee not found.");

        if (cmd.ReportingToEmployeeId is int rid)
        {
            if (await _repo.GetByIdAsync(rid, ct) is null) return ApiResponse<int>.Fail("Reporting-to employee not found.");
        }

        e.PhotoUrl = Clean(cmd.PhotoUrl);
        e.BloodGroup = Clean(cmd.BloodGroup);
        e.MaritalStatus = Enum.Parse<MaritalStatus>(cmd.MaritalStatus);
        e.Religion = Clean(cmd.Religion);
        e.Nationality = Clean(cmd.Nationality);
        e.WorkLocation = Clean(cmd.WorkLocation);
        e.AboutMe = Clean(cmd.AboutMe);
        e.ProbationEndDate = cmd.ProbationEndDate;
        e.ConfirmationDate = cmd.ConfirmationDate;
        e.ReportingToEmployeeId = cmd.ReportingToEmployeeId;
        e.UserId = Clean(cmd.UserId);

        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Profile updated.");
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
