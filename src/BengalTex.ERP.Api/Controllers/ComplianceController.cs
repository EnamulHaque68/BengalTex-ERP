using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Compliance.Commands;
using BengalTex.ERP.Application.Compliance.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/compliance-certificates")]
[Authorize]
public class ComplianceCertificatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ComplianceCertificatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Compliance.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? certificateType = null,
        [FromQuery] string? expiryStatus = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCertificatesQuery(parameters, certificateType, expiryStatus, includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Compliance.ManageCertificates)]
    public async Task<IActionResult> Create([FromBody] CreateCertificateRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCertificateCommand(
            req.Name, req.CertificateType, req.IssuingAuthority, req.CertificateNumber,
            req.IssuedDate, req.ExpiryDate, req.Notes), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Compliance.ManageCertificates)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCertificateRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateCertificateCommand(
            id, req.Name, req.CertificateType, req.IssuingAuthority, req.CertificateNumber,
            req.IssuedDate, req.ExpiryDate, req.Notes, req.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Compliance.ManageCertificates)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteCertificateCommand(id), ct));
}

public record CreateCertificateRequest(string Name, string CertificateType,
    string? IssuingAuthority, string? CertificateNumber,
    DateOnly IssuedDate, DateOnly ExpiryDate, string? Notes);

public record UpdateCertificateRequest(string Name, string CertificateType,
    string? IssuingAuthority, string? CertificateNumber,
    DateOnly IssuedDate, DateOnly ExpiryDate, string? Notes, bool IsActive);


[ApiController]
[Route("api/compliance-audits")]
[Authorize]
public class ComplianceAuditsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ComplianceAuditsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Compliance.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? auditType = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetAuditsQuery(parameters, auditType, status, fromDate, toDate), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Compliance.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAuditByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Compliance.ScheduleAudit)]
    public async Task<IActionResult> Create([FromBody] CreateAuditRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateAuditCommand(req.AuditType, req.Auditor, req.ScheduledDate, req.Notes), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Compliance.RecordAudit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAuditRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateAuditCommand(
            id, req.Auditor, req.ScheduledDate, req.ActualDate,
            req.Status, req.Result, req.Score, req.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Compliance.ScheduleAudit)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteAuditCommand(id), ct));

    // ── Findings (CAP items) ──
    [HttpPost("{id:long}/findings")]
    [HasPermission(Permissions.Compliance.RecordAudit)]
    public async Task<IActionResult> AddFinding(long id, [FromBody] AddFindingRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new AddAuditFindingCommand(
            id, req.FindingDescription, req.Severity, req.CorrectiveAction,
            req.AssignedToEmployeeId, req.DueDate, req.Notes), ct));

    [HttpPut("findings/{findingId:long}")]
    [HasPermission(Permissions.Compliance.ManageCap)]
    public async Task<IActionResult> UpdateFinding(long findingId, [FromBody] UpdateFindingRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateAuditFindingCommand(
            findingId, req.FindingDescription, req.Severity, req.CorrectiveAction,
            req.AssignedToEmployeeId, req.DueDate, req.Status, req.ClosureDate, req.Notes), ct));

    [HttpDelete("findings/{findingId:long}")]
    [HasPermission(Permissions.Compliance.ManageCap)]
    public async Task<IActionResult> DeleteFinding(long findingId, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteAuditFindingCommand(findingId), ct));
}

public record CreateAuditRequest(string AuditType, string Auditor, DateOnly ScheduledDate, string? Notes);

public record UpdateAuditRequest(string Auditor, DateOnly ScheduledDate, DateOnly? ActualDate,
    string Status, string? Result, decimal? Score, string? Notes);

public record AddFindingRequest(string FindingDescription, string Severity, string? CorrectiveAction,
    int? AssignedToEmployeeId, DateOnly? DueDate, string? Notes);

public record UpdateFindingRequest(string FindingDescription, string Severity, string? CorrectiveAction,
    int? AssignedToEmployeeId, DateOnly? DueDate, string Status, DateOnly? ClosureDate, string? Notes);


[ApiController]
[Route("api/compliance-dashboard")]
[Authorize]
public class ComplianceDashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public ComplianceDashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Compliance.View)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _mediator.Send(new GetComplianceDashboardQuery(), ct));
}
