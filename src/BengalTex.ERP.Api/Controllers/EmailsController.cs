using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Emails.Commands;
using BengalTex.ERP.Application.Emails.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/emails")]
[Authorize]
public class EmailsController : ControllerBase
{
    private readonly IMediator _mediator;
    public EmailsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Sent-email audit log (paginated).</summary>
    [HttpGet]
    [HasPermission(Permissions.Emails.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] long? sourceId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSentEmailsQuery(parameters, status, sourceType, sourceId), ct));

    /// <summary>
    /// Pre-render an email body + subject + default recipient for a document so the UI
    /// dialog can pre-fill itself. Caller then edits and POSTs to <c>send-document</c>.
    /// </summary>
    [HttpGet("preview")]
    [HasPermission(Permissions.Emails.Send)]
    public async Task<IActionResult> Preview(
        [FromQuery] string sourceType,
        [FromQuery] long sourceId,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetEmailPreviewQuery(sourceType, sourceId), ct));

    /// <summary>Send a document via email. Logs every attempt (sent OR failed) to the audit table.</summary>
    [HttpPost("send-document")]
    [HasPermission(Permissions.Emails.Send)]
    public async Task<IActionResult> SendDocument([FromBody] SendDocumentEmailCommand cmd, CancellationToken ct)
        => Ok(await _mediator.Send(cmd, ct));
}
