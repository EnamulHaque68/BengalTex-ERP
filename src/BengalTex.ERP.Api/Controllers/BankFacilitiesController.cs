using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Banking.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Phase A6c — bank treasury facilities (term loan / OD-CC / FDR) and their financial events
/// (drawdown, interest, repayment / placement, income, encashment) each posting a journal.
/// </summary>
[ApiController]
[Route("api/bank-facilities")]
[Authorize]
public class BankFacilitiesController : ControllerBase
{
    private readonly IMediator _mediator;
    public BankFacilitiesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Banking.View)]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBankFacilitiesQuery(status), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Banking.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetBankFacilityByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Banking.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateBankFacilityCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("{id:long}/events")]
    [HasPermission(Permissions.Banking.Manage)]
    public async Task<IActionResult> AddEvent(long id, [FromBody] AddBankFacilityEventRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new AddBankFacilityEventCommand(
            id, request.EventType, request.EventDate, request.Amount,
            request.PaymentMethod, request.Reference, request.Notes), ct));
}

public record AddBankFacilityEventRequest(
    string EventType, DateOnly EventDate, decimal Amount, string PaymentMethod, string? Reference, string? Notes);
