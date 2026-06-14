using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Expenses.Commands;
using BengalTex.ERP.Application.Expenses.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExpensesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetExpensesQuery(parameters, categoryId, status, fromDate, toDate), ct));

    [HttpGet("summary")]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<IActionResult> Summary([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
        => Ok(await _mediator.Send(new GetExpenseSummaryQuery(fromDate, toDate), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetExpenseByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Expenses.Create)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Expenses.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateExpenseCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Expenses.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteExpenseCommand(id), ct));

    /// <summary>
    /// Submit a draft expense for approval. Within the configured threshold it is recorded +
    /// paid immediately; above it, it goes to PendingApproval for sign-off via the Approvals inbox.
    /// </summary>
    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.Expenses.Approve)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new SubmitExpenseForApprovalCommand(id), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Expenses.Approve)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelExpenseCommand(id), ct));
}

[ApiController]
[Route("api/expense-categories")]
[Authorize]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExpenseCategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetExpenseCategoriesQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Expenses.ManageCategories)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCategoryCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Expenses.ManageCategories)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseCategoryCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Expenses.ManageCategories)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteExpenseCategoryCommand(id), ct));
}
