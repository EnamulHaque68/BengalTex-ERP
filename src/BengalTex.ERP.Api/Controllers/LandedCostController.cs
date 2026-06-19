using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.LandedCost;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Landed-cost vouchers — capitalise import charges (freight/duty/clearing/insurance) onto a
/// posted goods receipt's raw materials, raising their weighted-average cost. Reuses the
/// GoodsReceipts permission group.
/// </summary>
[ApiController]
[Route("api/landed-cost")]
[Authorize]
public class LandedCostController : ControllerBase
{
    private readonly IMediator _mediator;
    public LandedCostController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.GoodsReceipts.View)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryParameters parameters, [FromQuery] string? status = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetLandedCostVouchersQuery(parameters, status), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.GoodsReceipts.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetLandedCostVoucherByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.GoodsReceipts.Create)]
    public async Task<IActionResult> Create([FromBody] CreateLandedCostVoucherCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.GoodsReceipts.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateLandedCostVoucherCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.GoodsReceipts.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteLandedCostVoucherCommand(id), ct));

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.GoodsReceipts.Post)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new PostLandedCostVoucherCommand(id), ct));
}
