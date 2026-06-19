using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.SupplierQuotations;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Supplier quotations (RFQ) — collect competing supplier price quotes (optionally against a
/// purchase requisition), compare them side-by-side, then select the winner to generate a PO.
/// Reuses the PurchaseOrders permission group.
/// </summary>
[ApiController]
[Route("api/supplier-quotations")]
[Authorize]
public class SupplierQuotationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SupplierQuotationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.PurchaseOrders.View)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryParameters parameters, [FromQuery] string? status = null,
        [FromQuery] int? supplierId = null, [FromQuery] long? purchaseRequisitionId = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSupplierQuotationsQuery(parameters, status, supplierId, purchaseRequisitionId), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.PurchaseOrders.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSupplierQuotationByIdQuery(id), ct));

    [HttpGet("comparison/{purchaseRequisitionId:long}")]
    [HasPermission(Permissions.PurchaseOrders.View)]
    public async Task<IActionResult> Comparison(long purchaseRequisitionId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetQuotationComparisonQuery(purchaseRequisitionId), ct));

    [HttpPost]
    [HasPermission(Permissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierQuotationCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSupplierQuotationCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.PurchaseOrders.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteSupplierQuotationCommand(id), ct));

    [HttpPost("{id:long}/submit")]
    [HasPermission(Permissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new SubmitSupplierQuotationCommand(id), ct));

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Reject(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new RejectSupplierQuotationCommand(id), ct));

    [HttpPost("{id:long}/select")]
    [HasPermission(Permissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Select(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new SelectSupplierQuotationCommand(id), ct));
}
