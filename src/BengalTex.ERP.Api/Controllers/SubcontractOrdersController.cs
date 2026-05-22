using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Subcontract.Commands;
using BengalTex.ERP.Application.Subcontract.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/subcontract-orders")]
[Authorize]
public class SubcontractOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubcontractOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Subcontracting.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? subcontractorId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSubcontractOrdersQuery(parameters, subcontractorId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Subcontracting.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubcontractOrderByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Subcontracting.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSubcontractOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSubcontractOrderCommand(
            request.SubcontractorId, request.OrderDate, request.ExpectedReturnDate, request.ProcessType,
            request.WarehouseId, request.ChargeAmount, request.Notes, request.Lines), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Subcontracting.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSubcontractOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSubcontractOrderCommand(
            id, request.SubcontractorId, request.OrderDate, request.ExpectedReturnDate, request.ProcessType,
            request.WarehouseId, request.ChargeAmount, request.Notes, request.Lines), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Subcontracting.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteSubcontractOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/issue")]
    [HasPermission(Permissions.Subcontracting.Issue)]
    public async Task<IActionResult> Issue(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new IssueSubcontractOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/receive")]
    [HasPermission(Permissions.Subcontracting.Receive)]
    public async Task<IActionResult> Receive(long id, [FromBody] ReceiveSubcontractOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReceiveSubcontractOrderCommand(id, request.Lines), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Subcontracting.Edit)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelSubcontractOrderCommand(id), ct);
        return Ok(result);
    }
}

public record CreateSubcontractOrderRequest(
    int SubcontractorId,
    DateOnly OrderDate,
    DateOnly? ExpectedReturnDate,
    string ProcessType,
    int WarehouseId,
    decimal ChargeAmount,
    string? Notes,
    IReadOnlyList<SubcontractLineInput> Lines);

public record UpdateSubcontractOrderRequest(
    int SubcontractorId,
    DateOnly OrderDate,
    DateOnly? ExpectedReturnDate,
    string ProcessType,
    int WarehouseId,
    decimal ChargeAmount,
    string? Notes,
    IReadOnlyList<SubcontractLineInput> Lines);

public record ReceiveSubcontractOrderRequest(
    IReadOnlyList<SubcontractReceiveLineInput> Lines);
