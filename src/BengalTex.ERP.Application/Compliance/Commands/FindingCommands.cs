using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Compliance.Commands;

// ── Add finding to an audit ──
public sealed record AddAuditFindingCommand(
    long ComplianceAuditId,
    string FindingDescription,
    string Severity,
    string? CorrectiveAction,
    int? AssignedToEmployeeId,
    DateOnly? DueDate,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class AddAuditFindingCommandValidator : AbstractValidator<AddAuditFindingCommand>
{
    public AddAuditFindingCommandValidator()
    {
        RuleFor(x => x.ComplianceAuditId).GreaterThan(0);
        RuleFor(x => x.FindingDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Severity).NotEmpty().Must(s => Enum.TryParse<AuditFindingSeverity>(s, out _));
        RuleFor(x => x.CorrectiveAction).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class AddAuditFindingCommandHandler : IRequestHandler<AddAuditFindingCommand, ApiResponse<long>>
{
    private readonly IRepository<AuditFinding, long> _repo;
    private readonly IRepository<ComplianceAudit, long> _auditRepo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;

    public AddAuditFindingCommandHandler(
        IRepository<AuditFinding, long> repo,
        IRepository<ComplianceAudit, long> auditRepo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow,
        INotificationService notifications)
    { _repo = repo; _auditRepo = auditRepo; _empRepo = empRepo; _uow = uow; _notifications = notifications; }

    public async Task<ApiResponse<long>> Handle(AddAuditFindingCommand cmd, CancellationToken ct)
    {
        var audit = await _auditRepo.GetByIdAsync(cmd.ComplianceAuditId, ct);
        if (audit is null) return ApiResponse<long>.Fail("Audit not found.");

        Domain.Entities.Employee? assigned = null;
        if (cmd.AssignedToEmployeeId is int eid)
        {
            assigned = await _empRepo.GetByIdAsync(eid, ct);
            if (assigned is null || !assigned.IsActive)
                return ApiResponse<long>.Fail("Assigned employee not found or inactive.");
        }

        var f = new AuditFinding
        {
            ComplianceAuditId = cmd.ComplianceAuditId,
            FindingDescription = cmd.FindingDescription.Trim(),
            Severity = Enum.Parse<AuditFindingSeverity>(cmd.Severity),
            CorrectiveAction = string.IsNullOrWhiteSpace(cmd.CorrectiveAction) ? null : cmd.CorrectiveAction.Trim(),
            AssignedToEmployeeId = cmd.AssignedToEmployeeId,
            DueDate = cmd.DueDate,
            Status = AuditFindingStatus.Open,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(f, ct);

        // Notify the assigned employee — they own the CAP item now.
        if (assigned is not null)
        {
            var dueSuffix = cmd.DueDate.HasValue ? $" Due by {cmd.DueDate.Value:yyyy-MM-dd}." : "";
            await _notifications.NotifyAsync(
                NotificationChannels.InApp,
                recipient: assigned.FullName,
                subject: $"CAP item assigned ({audit.Code})",
                body: $"A {f.Severity} finding was assigned to you on audit {audit.Code}: " +
                      $"{f.FindingDescription}.{dueSuffix}",
                relatedType: "AuditFinding", relatedId: 0, ct: ct);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(f.Id, "Finding added.");
    }
}

// ── Update finding (CAP edit) ──
public sealed record UpdateAuditFindingCommand(
    long Id,
    string FindingDescription,
    string Severity,
    string? CorrectiveAction,
    int? AssignedToEmployeeId,
    DateOnly? DueDate,
    string Status,
    DateOnly? ClosureDate,
    string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateAuditFindingCommandValidator : AbstractValidator<UpdateAuditFindingCommand>
{
    public UpdateAuditFindingCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.FindingDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Severity).NotEmpty().Must(s => Enum.TryParse<AuditFindingSeverity>(s, out _));
        RuleFor(x => x.Status).NotEmpty().Must(s => Enum.TryParse<AuditFindingStatus>(s, out _));
        RuleFor(x => x.CorrectiveAction).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateAuditFindingCommandHandler : IRequestHandler<UpdateAuditFindingCommand, ApiResponse>
{
    private readonly IRepository<AuditFinding, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public UpdateAuditFindingCommandHandler(
        IRepository<AuditFinding, long> repo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    { _repo = repo; _empRepo = empRepo; _uow = uow; _clock = clock; }

    public async Task<ApiResponse> Handle(UpdateAuditFindingCommand cmd, CancellationToken ct)
    {
        var f = await _repo.GetByIdAsync(cmd.Id, ct);
        if (f is null) return ApiResponse.Fail("Finding not found.");
        if (cmd.AssignedToEmployeeId is int eid
            && !await _empRepo.Query().AnyAsync(e => e.Id == eid && e.IsActive, ct))
            return ApiResponse.Fail("Assigned employee not found or inactive.");

        f.FindingDescription = cmd.FindingDescription.Trim();
        f.Severity = Enum.Parse<AuditFindingSeverity>(cmd.Severity);
        f.CorrectiveAction = string.IsNullOrWhiteSpace(cmd.CorrectiveAction) ? null : cmd.CorrectiveAction.Trim();
        f.AssignedToEmployeeId = cmd.AssignedToEmployeeId;
        f.DueDate = cmd.DueDate;

        var newStatus = Enum.Parse<AuditFindingStatus>(cmd.Status);
        // Auto-set ClosureDate when status transitions to Closed/Waived and none provided
        if ((newStatus == AuditFindingStatus.Closed || newStatus == AuditFindingStatus.Waived)
            && cmd.ClosureDate is null && f.ClosureDate is null)
        {
            f.ClosureDate = _clock.Today;
        }
        else
        {
            f.ClosureDate = cmd.ClosureDate;
        }
        f.Status = newStatus;
        f.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();

        _repo.Update(f);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Finding updated.");
    }
}

// ── Delete finding ──
public sealed record DeleteAuditFindingCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteAuditFindingCommandHandler : IRequestHandler<DeleteAuditFindingCommand, ApiResponse>
{
    private readonly IRepository<AuditFinding, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteAuditFindingCommandHandler(IRepository<AuditFinding, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteAuditFindingCommand cmd, CancellationToken ct)
    {
        var f = await _repo.GetByIdAsync(cmd.Id, ct);
        if (f is null) return ApiResponse.Fail("Finding not found.");
        _repo.Remove(f);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Finding deleted.");
    }
}
