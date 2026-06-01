using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.BankReconciliation.Commands;
using BengalTex.ERP.Application.BankReconciliation.Queries;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/bank-statements")]
[Authorize]
public class BankStatementsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BankStatementsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.BankReconciliation.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? bankAccountId = null,
        [FromQuery] bool? isReconciled = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBankStatementsQuery(parameters, bankAccountId, isReconciled), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.BankReconciliation.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetBankStatementByIdQuery(id), ct));

    [HttpGet("{id:long}/unmatched-journal-lines")]
    [HasPermission(Permissions.BankReconciliation.View)]
    public async Task<IActionResult> UnmatchedJournalLines(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUnmatchedJournalLinesQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateBankStatementRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateBankStatementCommand(
            req.BankAccountId, req.StatementDate, req.PeriodFromDate, req.PeriodToDate,
            req.OpeningBalance, req.ClosingBalance, req.Notes), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateBankStatementRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateBankStatementCommand(
            id, req.StatementDate, req.PeriodFromDate, req.PeriodToDate,
            req.OpeningBalance, req.ClosingBalance, req.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteBankStatementCommand(id), ct));

    [HttpPost("{id:long}/reconcile")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> Reconcile(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new ReconcileBankStatementCommand(id), ct));

    // ── Lines (nested sub-resource) ──
    [HttpPost("{id:long}/lines")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> AddLine(long id, [FromBody] AddStatementLineRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new AddStatementLineCommand(
            id, req.TransactionDate, req.Description, req.ReferenceNumber, req.Amount, req.Notes), ct));

    [HttpPut("lines/{lineId:long}")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> UpdateLine(long lineId, [FromBody] UpdateStatementLineRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateStatementLineCommand(
            lineId, req.TransactionDate, req.Description, req.ReferenceNumber, req.Amount, req.Notes), ct));

    [HttpDelete("lines/{lineId:long}")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> DeleteLine(long lineId, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteStatementLineCommand(lineId), ct));

    [HttpPost("lines/{lineId:long}/match")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> MatchLine(long lineId, [FromBody] MatchLineRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new MatchStatementLineCommand(lineId, req.JournalLineId), ct));

    [HttpPost("lines/{lineId:long}/unmatch")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> UnmatchLine(long lineId, CancellationToken ct)
        => Ok(await _mediator.Send(new UnmatchStatementLineCommand(lineId), ct));

    [HttpPost("lines/{lineId:long}/exclude")]
    [HasPermission(Permissions.BankReconciliation.Manage)]
    public async Task<IActionResult> ExcludeLine(long lineId, [FromBody] ExcludeLineRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new ExcludeStatementLineCommand(lineId, req.Notes), ct));
}

public record CreateBankStatementRequest(int BankAccountId, DateOnly StatementDate,
    DateOnly PeriodFromDate, DateOnly PeriodToDate,
    decimal OpeningBalance, decimal ClosingBalance, string? Notes);

public record UpdateBankStatementRequest(DateOnly StatementDate,
    DateOnly PeriodFromDate, DateOnly PeriodToDate,
    decimal OpeningBalance, decimal ClosingBalance, string? Notes);

public record AddStatementLineRequest(DateOnly TransactionDate, string Description,
    string? ReferenceNumber, decimal Amount, string? Notes);

public record UpdateStatementLineRequest(DateOnly TransactionDate, string Description,
    string? ReferenceNumber, decimal Amount, string? Notes);

public record MatchLineRequest(long JournalLineId);
public record ExcludeLineRequest(string? Notes);
