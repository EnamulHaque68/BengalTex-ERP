using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Bom.Commands;
using BengalTex.ERP.Application.Bom.Queries;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/boms")]
[Authorize]
public class BomsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BomsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Boms.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? productId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBomsQuery(parameters, productId, status, activeOnly), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Boms.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBomByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Boms.Create)]
    public async Task<IActionResult> Create([FromBody] CreateBomRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBomCommand(
            request.ProductId, request.Name, request.OutputQuantity,
            request.EffectiveDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Boms.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBomRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateBomCommand(
            id, request.Name, request.OutputQuantity,
            request.EffectiveDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Boms.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteBomCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    [HasPermission(Permissions.Boms.Approve)]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApproveBomCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:int}/activate")]
    [HasPermission(Permissions.Boms.Approve)]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ActivateBomCommand(id), ct);
        return Ok(result);
    }
}

public record CreateBomRequest(
    int ProductId,
    string? Name,
    decimal OutputQuantity,
    DateOnly? EffectiveDate,
    string? Notes,
    IReadOnlyList<BomLineInput> Lines);

public record UpdateBomRequest(
    string? Name,
    decimal OutputQuantity,
    DateOnly? EffectiveDate,
    string? Notes,
    IReadOnlyList<BomLineInput> Lines);
