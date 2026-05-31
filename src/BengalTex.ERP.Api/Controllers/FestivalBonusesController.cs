using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/festival-bonuses")]
[Authorize]
public class FestivalBonusesController : ControllerBase
{
    private readonly IMediator _mediator;
    public FestivalBonusesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.FestivalBonuses.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? year = null,
        [FromQuery] string? bonusType = null,
        [FromQuery] string? status = null,
        [FromQuery] int? employeeId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetFestivalBonusesQuery(parameters, year, bonusType, status, employeeId), ct));

    [HttpPost("bulk-create")]
    [HasPermission(Permissions.FestivalBonuses.Create)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateFestivalBonusRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new BulkCreateFestivalBonusCommand(req.BonusYear, req.BonusType, req.Amount, req.Notes), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.FestivalBonuses.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateFestivalBonusRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateFestivalBonusCommand(id, req.Amount, req.PaymentMethod, req.Notes), ct));

    [HttpPost("{id:long}/pay")]
    [HasPermission(Permissions.FestivalBonuses.Pay)]
    public async Task<IActionResult> Pay(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new PayFestivalBonusCommand(id), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.FestivalBonuses.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteFestivalBonusCommand(id), ct));
}

public record BulkCreateFestivalBonusRequest(int BonusYear, string BonusType, decimal Amount, string? Notes);
public record UpdateFestivalBonusRequest(decimal Amount, string PaymentMethod, string? Notes);
