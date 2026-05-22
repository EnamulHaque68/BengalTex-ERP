using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Style.Commands;
using BengalTex.ERP.Application.Style.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/styles")]
[Authorize]
public class StylesController : ControllerBase
{
    private readonly IMediator _mediator;

    public StylesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Styles.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int? buyerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStylesQuery(parameters, includeInactive, buyerId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Styles.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStyleByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Styles.Create)]
    public async Task<IActionResult> Create([FromBody] CreateStyleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateStyleCommand(
            request.Code, request.StyleName, request.BuyerId, request.ProductId,
            request.BuyerStyleRef, request.Season, request.Status, request.Description, request.Notes), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Styles.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStyleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateStyleCommand(
            id, request.StyleName, request.BuyerId, request.ProductId,
            request.BuyerStyleRef, request.Season, request.Status, request.Description, request.Notes, request.IsActive), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Styles.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteStyleCommand(id), ct);
        return Ok(result);
    }
}

public record CreateStyleRequest(
    string? Code,
    string StyleName,
    int BuyerId,
    int? ProductId,
    string? BuyerStyleRef,
    string? Season,
    string Status,
    string? Description,
    string? Notes);

public record UpdateStyleRequest(
    string StyleName,
    int BuyerId,
    int? ProductId,
    string? BuyerStyleRef,
    string? Season,
    string Status,
    string? Description,
    string? Notes,
    bool IsActive);
