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

    /// <summary>Cash Book — chronological ledger of seeded Cash account (1110) with running balance.</summary>
    [HttpGet("cash-book")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> CashBook(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCashBookQuery(fromDate, toDate), ct));

    /// <summary>
    /// Bank Book — chronological ledger for one BankAccount entity (via its LedgerAccountId)
    /// or aggregate over the seeded Bank ledger (1120) when bankAccountId is omitted.
    /// </summary>
    [HttpGet("bank-book")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> BankBook(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] int? bankAccountId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBankBookQuery(bankAccountId, fromDate, toDate), ct));

    /// <summary>Day Book — every posted journal voucher in a date range, with all line legs.</summary>
    [HttpGet("day-book")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> DayBook(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDayBookQuery(fromDate, toDate), ct));
}
