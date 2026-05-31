using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Commands;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? accountType = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] bool? postableOnly = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAccountsQuery(accountType, includeInactive, postableOnly, search), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAccountByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Accounting.ManageAccounts)]
    public async Task<IActionResult> Create([FromBody] CreateAccountCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Accounting.ManageAccounts)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Accounting.ManageAccounts)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteAccountCommand(id), ct));

    // ── Reports ──
    [HttpGet("trial-balance")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> TrialBalance([FromQuery] DateOnly? asOfDate = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetTrialBalanceQuery(asOfDate), ct));

    [HttpGet("{id:int}/ledger")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GeneralLedger(
        int id, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
        => Ok(await _mediator.Send(new GetGeneralLedgerQuery(id, fromDate, toDate), ct));

    [HttpGet("profit-loss")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> ProfitAndLoss(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProfitAndLossQuery(fromDate, toDate), ct));

    [HttpGet("balance-sheet")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateOnly? asOfDate = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBalanceSheetQuery(asOfDate), ct));

    [HttpGet("cash-flow")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> CashFlow(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCashFlowStatementQuery(fromDate, toDate), ct));
}
