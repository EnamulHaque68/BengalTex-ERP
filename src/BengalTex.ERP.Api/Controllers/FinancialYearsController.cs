using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Fiscal;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>Phase A1 — fiscal years, accounting periods, year-end close and opening balances.</summary>
[ApiController]
[Route("api/financial-years")]
[Authorize]
public class FinancialYearsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinancialYearsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetFinancialYearsQuery(), ct));

    [HttpPost]
    [HasPermission(Permissions.Accounting.ManagePeriods)]
    public async Task<IActionResult> Create([FromBody] CreateFinancialYearRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateFinancialYearCommand(request.Code, request.StartDate, request.Notes), ct));

    /// <summary>Income/expense totals the year-end close would sweep — shown before confirming.</summary>
    [HttpGet("{id:int}/close-preview")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> ClosePreview(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetYearClosePreviewQuery(id), ct));

    /// <summary>Year-end close → Retained Earnings (all periods must be locked).</summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new CloseFinancialYearCommand(id), ct));

    /// <summary>Audited reopen — reverses the closing voucher.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Reopen(int id, [FromBody] ReopenYearRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ReopenFinancialYearCommand(id, request.Reason), ct));

    /// <summary>
    /// Period lifecycle: periodAction = soft-close | lock | reopen.
    /// NOTE: the route parameter must NOT be named "action" — that is a reserved MVC routing
    /// value; using it makes the framework match it against the action name and the endpoint
    /// silently 404s.
    /// </summary>
    [HttpPost("periods/{periodId:int}/{periodAction}")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> ChangePeriodStatus(int periodId, string periodAction, CancellationToken ct)
        => Ok(await _mediator.Send(new ChangePeriodStatusCommand(periodId, periodAction), ct));

    // ── Opening balances (D5) ──

    /// <summary>Postable accounts + already-imported opening amounts — the import grid.</summary>
    [HttpGet("opening-balances/template")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> OpeningTemplate(CancellationToken ct)
        => Ok(await _mediator.Send(new GetOpeningBalanceTemplateQuery(), ct));

    /// <summary>Imports ledger opening balances as one posted Opening voucher (plug → 3150).</summary>
    [HttpPost("opening-balances/import")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> ImportOpeningBalances(
        [FromBody] ImportOpeningBalancesRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ImportOpeningBalancesCommand(request.AsOfDate, request.Lines), ct));
}

public record CreateFinancialYearRequest(string Code, DateOnly StartDate, string? Notes);
public record ReopenYearRequest(string Reason);
public record ImportOpeningBalancesRequest(DateOnly AsOfDate, IReadOnlyList<OpeningBalanceLineInput> Lines);
