using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.PurchaseOrder.Commands;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PurchaseOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.PurchaseOrders.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? supplierId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery(parameters, supplierId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.PurchaseOrders.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPurchaseOrderByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePurchaseOrderCommand(
            request.SupplierId, request.OrderDate, request.ExpectedDeliveryDate,
            request.DeliveryWarehouseId, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePurchaseOrderCommand(
            id, request.SupplierId, request.OrderDate, request.ExpectedDeliveryDate,
            request.DeliveryWarehouseId, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.PurchaseOrders.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePurchaseOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/submit-for-approval")]
    [HasPermission(Permissions.PurchaseOrders.Approve)]
    public async Task<IActionResult> SubmitForApproval(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitPurchaseOrderForApprovalCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/send")]
    [HasPermission(Permissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Send(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SendPurchaseOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelPurchaseOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/close")]
    [HasPermission(Permissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Close(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ClosePurchaseOrderCommand(id), ct);
        return Ok(result);
    }
}

public record CreatePurchaseOrderRequest(
    int SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int? DeliveryWarehouseId,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineInput> Lines);

public record UpdatePurchaseOrderRequest(
    int SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int? DeliveryWarehouseId,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineInput> Lines);
