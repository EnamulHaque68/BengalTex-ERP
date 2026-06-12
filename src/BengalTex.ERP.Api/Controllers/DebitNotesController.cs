using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.DebitNotes.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/debit-notes")]
[Authorize]
public class DebitNotesController : ControllerBase
{
    private readonly IMediator _mediator;
    public DebitNotesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.DebitNotes.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] long? supplierInvoiceId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDebitNotesQuery(parameters, status, supplierId, supplierInvoiceId), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.DebitNotes.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDebitNoteByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.DebitNotes.Create)]
    public async Task<IActionResult> Create([FromBody] CreateDebitNoteRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateDebitNoteCommand(
            req.SupplierInvoiceId, req.IssueDate, req.Reason, req.Amount, req.Notes,
            req.SupplierReturnNoteId), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.DebitNotes.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDebitNoteRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateDebitNoteCommand(
            id, req.IssueDate, req.Reason, req.Amount, req.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.DebitNotes.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteDebitNoteCommand(id), ct));

    [HttpPost("{id:long}/issue")]
    [HasPermission(Permissions.DebitNotes.Issue)]
    public async Task<IActionResult> Issue(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new IssueDebitNoteCommand(id), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.DebitNotes.Issue)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelDebitNoteCommand(id), ct));
}

public record CreateDebitNoteRequest(long SupplierInvoiceId, DateOnly IssueDate,
    string Reason, decimal Amount, string? Notes, long? SupplierReturnNoteId = null);

public record UpdateDebitNoteRequest(DateOnly IssueDate, string Reason, decimal Amount, string? Notes);
