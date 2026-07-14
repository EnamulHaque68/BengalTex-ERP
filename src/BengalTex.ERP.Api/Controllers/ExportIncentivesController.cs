using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Banking.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Phase A6b — government export cash-incentive claims: accrue (Dr 1186 / Cr 4260), mark received
/// (Dr Bank / Cr 1186), or cancel (reverse the accrual).
/// </summary>
[ApiController]
[Route("api/export-incentives")]
[Authorize]
public class ExportIncentivesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExportIncentivesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Banking.View)]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetExportIncentiveClaimsQuery(status), ct));

    [HttpPost]
    [HasPermission(Permissions.Banking.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateExportIncentiveClaimCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("{id:long}/received")]
    [HasPermission(Permissions.Banking.Manage)]
    public async Task<IActionResult> MarkReceived(long id, [FromBody] MarkIncentiveReceivedRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new MarkIncentiveReceivedCommand(
            id, request.ReceivedDate, request.PaymentMethod, request.BankReference), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Banking.Manage)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelExportIncentiveClaimCommand(id), ct));
}

public record MarkIncentiveReceivedRequest(DateOnly ReceivedDate, string PaymentMethod, string? BankReference);
