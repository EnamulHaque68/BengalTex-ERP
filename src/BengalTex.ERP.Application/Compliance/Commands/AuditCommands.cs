using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Compliance.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Compliance.Commands;

// ── List ──
public sealed record GetAuditsQuery(
    PagedQueryParameters Parameters,
    string? AuditType = null,
    string? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<ComplianceAuditListItemDto>>>;

internal sealed class GetAuditsQueryHandler
    : IRequestHandler<GetAuditsQuery, ApiResponse<PagedResult<ComplianceAuditListItemDto>>>
{
    private readonly IRepository<ComplianceAudit, long> _repo;
    public GetAuditsQueryHandler(IRepository<ComplianceAudit, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<ComplianceAuditListItemDto>>> Handle(GetAuditsQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(request.AuditType)
            && Enum.TryParse<ComplianceAuditType>(request.AuditType, out var t))
            q = q.Where(a => a.AuditType == t);
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<ComplianceAuditStatus>(request.Status, out var s))
            q = q.Where(a => a.Status == s);
        if (request.FromDate.HasValue) q = q.Where(a => a.ScheduledDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) q = q.Where(a => a.ScheduledDate <= request.ToDate.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(a => a.Code.Contains(search) || a.Auditor.Contains(search));

        q = q.OrderByDescending(a => a.ScheduledDate);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(a => new ComplianceAuditListItemDto(
                a.Id, a.Code, a.AuditType.ToString(), a.Auditor,
                a.ScheduledDate, a.ActualDate, a.Status.ToString(),
                a.Result == null ? null : a.Result.ToString(),
                a.Score,
                a.Findings.Count(f => f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress)))
            .ToListAsync(ct);

        var result = PagedResult<ComplianceAuditListItemDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, total);
        return ApiResponse<PagedResult<ComplianceAuditListItemDto>>.Ok(result);
    }
}

// ── Get By Id ──
public sealed record GetAuditByIdQuery(long Id) : IRequest<ApiResponse<ComplianceAuditDto>>;

internal sealed class GetAuditByIdQueryHandler : IRequestHandler<GetAuditByIdQuery, ApiResponse<ComplianceAuditDto>>
{
    private readonly IRepository<ComplianceAudit, long> _repo;
    private readonly IDateTimeProvider _clock;
    public GetAuditByIdQueryHandler(IRepository<ComplianceAudit, long> repo, IDateTimeProvider clock)
    { _repo = repo; _clock = clock; }

    public async Task<ApiResponse<ComplianceAuditDto>> Handle(GetAuditByIdQuery request, CancellationToken ct)
    {
        var today = _clock.Today;
        var row = await _repo.Query()
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new
            {
                a.Id, a.Code, a.AuditType, a.Auditor, a.ScheduledDate, a.ActualDate,
                a.Status, a.Result, a.Score, a.Notes,
                Findings = a.Findings.OrderBy(f => f.Severity).ThenBy(f => f.DueDate).Select(f => new
                {
                    f.Id, f.ComplianceAuditId, f.FindingDescription, f.Severity, f.CorrectiveAction,
                    f.AssignedToEmployeeId,
                    AssignedToEmployeeName = f.AssignedToEmployee != null ? f.AssignedToEmployee.FullName : null,
                    f.DueDate, f.ClosureDate, f.Status, f.Notes
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return ApiResponse<ComplianceAuditDto>.Fail("Audit not found.");

        var findings = row.Findings.Select(f =>
        {
            var overdue = f.DueDate.HasValue && f.DueDate.Value < today
                          && f.Status != AuditFindingStatus.Closed && f.Status != AuditFindingStatus.Waived;
            return new AuditFindingDto(
                f.Id, f.ComplianceAuditId, f.FindingDescription, f.Severity.ToString(),
                f.CorrectiveAction, f.AssignedToEmployeeId, f.AssignedToEmployeeName,
                f.DueDate, f.ClosureDate, f.Status.ToString(), overdue, f.Notes);
        }).ToList();

        return ApiResponse<ComplianceAuditDto>.Ok(new ComplianceAuditDto(
            row.Id, row.Code, row.AuditType.ToString(), row.Auditor,
            row.ScheduledDate, row.ActualDate, row.Status.ToString(),
            row.Result?.ToString(), row.Score, row.Notes, findings));
    }
}

// ── Create ──
public sealed record CreateAuditCommand(
    string AuditType, string Auditor, DateOnly ScheduledDate, string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateAuditCommandValidator : AbstractValidator<CreateAuditCommand>
{
    public CreateAuditCommandValidator()
    {
        RuleFor(x => x.AuditType).NotEmpty().Must(s => Enum.TryParse<ComplianceAuditType>(s, out _));
        RuleFor(x => x.Auditor).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ScheduledDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateAuditCommandHandler : IRequestHandler<CreateAuditCommand, ApiResponse<long>>
{
    private readonly IRepository<ComplianceAudit, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    public CreateAuditCommandHandler(IRepository<ComplianceAudit, long> repo, IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateAuditCommand cmd, CancellationToken ct)
    {
        var code = await _numbering.NextAsync("AUD", null, ct);
        var a = new ComplianceAudit
        {
            Code = code,
            AuditType = Enum.Parse<ComplianceAuditType>(cmd.AuditType),
            Auditor = cmd.Auditor.Trim(),
            ScheduledDate = cmd.ScheduledDate,
            Status = ComplianceAuditStatus.Scheduled,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(a, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(a.Id, "Audit scheduled.");
    }
}

// ── Update result (after audit) ──
public sealed record UpdateAuditCommand(
    long Id, string Auditor, DateOnly ScheduledDate, DateOnly? ActualDate,
    string Status, string? Result, decimal? Score, string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateAuditCommandValidator : AbstractValidator<UpdateAuditCommand>
{
    public UpdateAuditCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Auditor).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).NotEmpty().Must(s => Enum.TryParse<ComplianceAuditStatus>(s, out _));
        RuleFor(x => x.Result)
            .Must(s => string.IsNullOrEmpty(s) || Enum.TryParse<ComplianceAuditResult>(s, out _))
            .WithMessage("Invalid Result.");
        RuleFor(x => x.Score).InclusiveBetween(0, 100).When(x => x.Score.HasValue);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateAuditCommandHandler : IRequestHandler<UpdateAuditCommand, ApiResponse>
{
    private readonly IRepository<ComplianceAudit, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateAuditCommandHandler(IRepository<ComplianceAudit, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateAuditCommand cmd, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(cmd.Id, ct);
        if (a is null) return ApiResponse.Fail("Audit not found.");
        a.Auditor = cmd.Auditor.Trim();
        a.ScheduledDate = cmd.ScheduledDate;
        a.ActualDate = cmd.ActualDate;
        a.Status = Enum.Parse<ComplianceAuditStatus>(cmd.Status);
        a.Result = string.IsNullOrEmpty(cmd.Result) ? null : Enum.Parse<ComplianceAuditResult>(cmd.Result);
        a.Score = cmd.Score;
        a.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(a);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Audit updated.");
    }
}

// ── Delete (Scheduled only) ──
public sealed record DeleteAuditCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteAuditCommandHandler : IRequestHandler<DeleteAuditCommand, ApiResponse>
{
    private readonly IRepository<ComplianceAudit, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteAuditCommandHandler(IRepository<ComplianceAudit, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteAuditCommand cmd, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(cmd.Id, ct);
        if (a is null) return ApiResponse.Fail("Audit not found.");
        if (a.Status != ComplianceAuditStatus.Scheduled)
            return ApiResponse.Fail($"Cannot delete a {a.Status} audit (cancel via status change instead).");
        _repo.Remove(a);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Audit deleted.");
    }
}
