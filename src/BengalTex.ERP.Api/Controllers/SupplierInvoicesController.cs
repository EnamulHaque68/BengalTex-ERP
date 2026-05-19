using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.SupplierInvoice.Commands;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/supplier-invoices")]
[Authorize]
public class SupplierInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupplierInvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? supplierId = null,
        [FromQuery] long? purchaseOrderId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetSupplierInvoicesQuery(parameters, supplierId, purchaseOrderId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSupplierInvoiceByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Invoices.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierInvoiceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSupplierInvoiceCommand(
            request.PurchaseOrderId, request.SupplierInvoiceNumber, request.VatRate, request.InvoiceDate,
            request.DueDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSupplierInvoiceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSupplierInvoiceCommand(
            id, request.SupplierInvoiceNumber, request.VatRate, request.InvoiceDate,
            request.DueDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Invoices.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteSupplierInvoiceCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApproveSupplierInvoiceCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelSupplierInvoiceCommand(id), ct);
        return Ok(result);
    }
}

public record CreateSupplierInvoiceRequest(
    long PurchaseOrderId,
    string? SupplierInvoiceNumber,
    decimal VatRate,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string? Notes,
    IReadOnlyList<SupplierInvoiceLineInput> Lines);

public record UpdateSupplierInvoiceRequest(
    string? SupplierInvoiceNumber,
    decimal VatRate,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<SupplierInvoiceLineInput> Lines);
