using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.CreditNotes.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/credit-notes")]
[Authorize]
public class CreditNotesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CreditNotesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.CreditNotes.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null,
        [FromQuery] long? customerInvoiceId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCreditNotesQuery(parameters, status, customerId, customerInvoiceId), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.CreditNotes.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCreditNoteByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.CreditNotes.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCreditNoteRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCreditNoteCommand(
            req.CustomerInvoiceId, req.IssueDate, req.Reason, req.Amount, req.Notes), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.CreditNotes.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCreditNoteRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateCreditNoteCommand(
            id, req.IssueDate, req.Reason, req.Amount, req.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.CreditNotes.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteCreditNoteCommand(id), ct));

    [HttpPost("{id:long}/issue")]
    [HasPermission(Permissions.CreditNotes.Issue)]
    public async Task<IActionResult> Issue(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new IssueCreditNoteCommand(id), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.CreditNotes.Issue)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelCreditNoteCommand(id), ct));
}

public record CreateCreditNoteRequest(long CustomerInvoiceId, DateOnly IssueDate,
    string Reason, decimal Amount, string? Notes);

public record UpdateCreditNoteRequest(DateOnly IssueDate, string Reason, decimal Amount, string? Notes);
