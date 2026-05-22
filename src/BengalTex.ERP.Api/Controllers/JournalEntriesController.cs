using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Commands;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/journal-entries")]
[Authorize]
public class JournalEntriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public JournalEntriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetJournalEntriesQuery(parameters, status, fromDate, toDate), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetJournalEntryByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Accounting.CreateJournal)]
    public async Task<IActionResult> Create([FromBody] CreateJournalEntryCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Accounting.CreateJournal)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJournalEntryCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Accounting.CreateJournal)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteJournalEntryCommand(id), ct));

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Accounting.PostJournal)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new PostJournalEntryCommand(id), ct));
}
