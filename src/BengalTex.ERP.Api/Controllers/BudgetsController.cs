using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Budgeting;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Phase A7a — annual budgets (per financial year, account &amp; cost center) and the
/// Budget-vs-Actual variance report against posted GL. Budgets post no journal (planning data).
/// </summary>
[ApiController]
[Route("api/budgets")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BudgetsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetAll([FromQuery] int? financialYearId = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBudgetsQuery(financialYearId), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetBudgetByIdQuery(id), ct));

    [HttpGet("{id:long}/variance")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> Variance(long id, [FromQuery] int fromMonth = 1, [FromQuery] int toMonth = 12,
        [FromQuery] int? costCenterId = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBudgetVarianceQuery(id, fromMonth, toMonth, costCenterId), ct));

    [HttpPost]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}/lines")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> SetLines(long id, [FromBody] SetBudgetLinesRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SetBudgetLinesCommand(id, request.Lines), ct));

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new ApproveBudgetCommand(id), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteBudgetCommand(id), ct));
}

public record SetBudgetLinesRequest(IReadOnlyList<BudgetLineInput> Lines);
