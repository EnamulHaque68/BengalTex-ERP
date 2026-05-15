using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.GoodsReceipt.Commands;
using BengalTex.ERP.Application.GoodsReceipt.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/goods-receipts")]
[Authorize]
public class GoodsReceiptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GoodsReceiptsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.GoodsReceipts.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] long? purchaseOrderId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetGoodsReceiptsQuery(parameters, purchaseOrderId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.GoodsReceipts.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGoodsReceiptByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.GoodsReceipts.Create)]
    public async Task<IActionResult> Create([FromBody] CreateGoodsReceiptRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateGoodsReceiptCommand(
            request.PurchaseOrderId, request.ReceiveDate, request.ReceivingWarehouseId,
            request.SupplierDeliveryRef, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.GoodsReceipts.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateGoodsReceiptRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateGoodsReceiptCommand(
            id, request.ReceiveDate, request.ReceivingWarehouseId,
            request.SupplierDeliveryRef, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.GoodsReceipts.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteGoodsReceiptCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.GoodsReceipts.Post)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostGoodsReceiptCommand(id), ct);
        return Ok(result);
    }
}

public record CreateGoodsReceiptRequest(
    long PurchaseOrderId,
    DateOnly ReceiveDate,
    int ReceivingWarehouseId,
    string? SupplierDeliveryRef,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineInput> Lines);

public record UpdateGoodsReceiptRequest(
    DateOnly ReceiveDate,
    int ReceivingWarehouseId,
    string? SupplierDeliveryRef,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineInput> Lines);
