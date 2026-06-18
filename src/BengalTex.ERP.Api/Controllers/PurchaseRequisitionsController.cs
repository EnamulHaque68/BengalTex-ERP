using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.PurchaseRequisitions.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/purchase-requisitions")]
[Authorize]
public class PurchaseRequisitionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PurchaseRequisitionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.PurchaseRequisitions.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] int? departmentId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetPurchaseRequisitionsQuery(parameters, status, departmentId), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.PurchaseRequisitions.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPurchaseRequisitionByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.PurchaseRequisitions.Create)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequisitionCommand cmd, CancellationToken ct)
        => Ok(await _mediator.Send(cmd, ct));

    /// <summary>Raise a Draft PR for a production order's raw-material shortfalls (BOM vs stock).</summary>
    [HttpPost("from-production/{productionOrderId:long}")]
    [HasPermission(Permissions.PurchaseRequisitions.Create)]
    public async Task<IActionResult> CreateFromProduction(long productionOrderId, CancellationToken ct)
        => Ok(await _mediator.Send(new GeneratePrFromProductionCommand(productionOrderId), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.PurchaseRequisitions.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePurchaseRequisitionBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdatePurchaseRequisitionCommand(
            id, body.RequisitionDate, body.NeededByDate, body.DepartmentId, body.DepartmentText,
            body.RequestedBy, body.Purpose, body.Notes, body.Lines), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.PurchaseRequisitions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeletePurchaseRequisitionCommand(id), ct));

    [HttpPost("{id:long}/submit")]
    [HasPermission(Permissions.PurchaseRequisitions.Submit)]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new SubmitPurchaseRequisitionCommand(id), ct));

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.PurchaseRequisitions.Decide)]
    public async Task<IActionResult> Approve(long id, [FromBody] DecisionBody? body, CancellationToken ct)
        => Ok(await _mediator.Send(new ApprovePurchaseRequisitionCommand(id, body?.Notes), ct));

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.PurchaseRequisitions.Decide)]
    public async Task<IActionResult> Reject(long id, [FromBody] DecisionBody? body, CancellationToken ct)
        => Ok(await _mediator.Send(new RejectPurchaseRequisitionCommand(id, body?.Notes), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.PurchaseRequisitions.Submit)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelPurchaseRequisitionCommand(id), ct));

    [HttpPost("{id:long}/convert")]
    [HasPermission(Permissions.PurchaseRequisitions.Convert)]
    public async Task<IActionResult> Convert(long id, [FromBody] ConvertPrBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new ConvertPurchaseRequisitionToPoCommand(
            id, body.SupplierId, body.OrderDate, body.ExpectedDeliveryDate, body.DeliveryWarehouseId,
            body.CurrencyId, body.ExchangeRate, body.Notes, body.LinePrices), ct));
}

public record UpdatePurchaseRequisitionBody(
    DateOnly RequisitionDate,
    DateOnly? NeededByDate,
    int? DepartmentId,
    string? DepartmentText,
    string? RequestedBy,
    string? Purpose,
    string? Notes,
    IReadOnlyList<PurchaseRequisitionLineInput> Lines);

public record DecisionBody(string? Notes);

public record ConvertPrBody(
    int SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int? DeliveryWarehouseId,
    int CurrencyId,
    decimal ExchangeRate,
    string? Notes,
    IReadOnlyList<ConvertPrLinePriceInput> LinePrices);
