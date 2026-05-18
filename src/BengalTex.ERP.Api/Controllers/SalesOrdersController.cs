using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.SalesOrder.Commands;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/sales-orders")]
[Authorize]
public class SalesOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.SalesOrders.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSalesOrdersQuery(parameters, customerId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.SalesOrders.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSalesOrderByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.SalesOrders.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSalesOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSalesOrderCommand(
            request.CustomerId, request.OrderDate, request.RequiredDeliveryDate,
            request.CustomerPoRef, request.DeliveryAddress, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.SalesOrders.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSalesOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSalesOrderCommand(
            id, request.CustomerId, request.OrderDate, request.RequiredDeliveryDate,
            request.CustomerPoRef, request.DeliveryAddress, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.SalesOrders.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteSalesOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/confirm")]
    [HasPermission(Permissions.SalesOrders.Confirm)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmSalesOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.SalesOrders.Cancel)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelSalesOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/close")]
    [HasPermission(Permissions.SalesOrders.Edit)]
    public async Task<IActionResult> Close(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CloseSalesOrderCommand(id), ct);
        return Ok(result);
    }
}

public record CreateSalesOrderRequest(
    int CustomerId,
    DateOnly OrderDate,
    DateOnly? RequiredDeliveryDate,
    string? CustomerPoRef,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<SalesOrderLineInput> Lines);

public record UpdateSalesOrderRequest(
    int CustomerId,
    DateOnly OrderDate,
    DateOnly? RequiredDeliveryDate,
    string? CustomerPoRef,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<SalesOrderLineInput> Lines);
