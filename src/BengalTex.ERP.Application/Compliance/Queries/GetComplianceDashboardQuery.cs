using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Compliance.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Compliance.Queries;

public sealed record GetComplianceDashboardQuery : IRequest<ApiResponse<ComplianceDashboardDto>>;

internal sealed class GetComplianceDashboardQueryHandler
    : IRequestHandler<GetComplianceDashboardQuery, ApiResponse<ComplianceDashboardDto>>
{
    private readonly IRepository<ComplianceCertificate> _certRepo;
    private readonly IRepository<ComplianceAudit, long> _auditRepo;
    private readonly IRepository<AuditFinding, long> _findingRepo;
    private readonly IDateTimeProvider _clock;

    public GetComplianceDashboardQueryHandler(
        IRepository<ComplianceCertificate> certRepo,
        IRepository<ComplianceAudit, long> auditRepo,
        IRepository<AuditFinding, long> findingRepo,
        IDateTimeProvider clock)
    {
        _certRepo = certRepo; _auditRepo = auditRepo;
        _findingRepo = findingRepo; _clock = clock;
    }

    public async Task<ApiResponse<ComplianceDashboardDto>> Handle(GetComplianceDashboardQuery request, CancellationToken ct)
    {
        var today = _clock.Today;
        var soonCutoff = today.AddDays(ExpiryStatus.ExpiringSoonDays);
        var auditCutoff = today.AddDays(30);

        // Certificates summary
        var certs = await _certRepo.Query()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id, c.Name, c.CertificateType, c.IssuingAuthority, c.CertificateNumber,
                c.IssuedDate, c.ExpiryDate, c.Notes, c.IsActive
            })
            .ToListAsync(ct);

        var active = 0; var soon = 0; var expired = 0;
        var expiringDtos = new List<ComplianceCertificateDto>();
        foreach (var c in certs)
        {
            var days = c.ExpiryDate.DayNumber - today.DayNumber;
            var status = ExpiryStatus.ClassifyDays(days);
            if (status == ExpiryStatus.Expired) expired++;
            else if (status == ExpiryStatus.ExpiringSoonStatus) soon++;
            else active++;

            if (status != ExpiryStatus.Active)
            {
                expiringDtos.Add(new ComplianceCertificateDto(
                    c.Id, c.Name, c.CertificateType.ToString(),
                    c.IssuingAuthority, c.CertificateNumber,
                    c.IssuedDate, c.ExpiryDate, days, status, c.Notes, c.IsActive));
            }
        }
        expiringDtos = expiringDtos.OrderBy(d => d.ExpiryDate).Take(10).ToList();

        // Findings
        var openFindingsCount = await _findingRepo.Query()
            .CountAsync(f => f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress, ct);

        var overdueRaw = await _findingRepo.Query()
            .Where(f => (f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress)
                        && f.DueDate.HasValue && f.DueDate.Value < today)
            .OrderBy(f => f.DueDate)
            .Take(10)
            .Select(f => new
            {
                f.Id, f.ComplianceAuditId, f.FindingDescription, f.Severity, f.CorrectiveAction,
                f.AssignedToEmployeeId,
                AssignedToEmployeeName = f.AssignedToEmployee != null ? f.AssignedToEmployee.FullName : null,
                f.DueDate, f.ClosureDate, f.Status, f.Notes
            })
            .ToListAsync(ct);
        var overdueCount = await _findingRepo.Query()
            .CountAsync(f => (f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress)
                             && f.DueDate.HasValue && f.DueDate.Value < today, ct);

        var overdueDtos = overdueRaw.Select(f => new AuditFindingDto(
            f.Id, f.ComplianceAuditId, f.FindingDescription, f.Severity.ToString(),
            f.CorrectiveAction, f.AssignedToEmployeeId, f.AssignedToEmployeeName,
            f.DueDate, f.ClosureDate, f.Status.ToString(), true, f.Notes)).ToList();

        // Upcoming audits (next 30 days, not yet completed)
        var upcomingAudits = await _auditRepo.Query()
            .CountAsync(a => a.Status == ComplianceAuditStatus.Scheduled
                             && a.ScheduledDate >= today && a.ScheduledDate <= auditCutoff, ct);

        return ApiResponse<ComplianceDashboardDto>.Ok(new ComplianceDashboardDto(
            active, soon, expired,
            openFindingsCount, overdueCount, upcomingAudits,
            expiringDtos, overdueDtos));
    }
}
