using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.CustomerInvoice.Commands;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/customer-invoices")]
[Authorize]
public class CustomerInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerInvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? customerId = null,
        [FromQuery] long? salesOrderId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetCustomerInvoicesQuery(parameters, customerId, salesOrderId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustomerInvoiceByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Invoices.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerInvoiceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCustomerInvoiceCommand(
            request.SalesOrderId, request.VatRate, request.InvoiceDate, request.DueDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerInvoiceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCustomerInvoiceCommand(
            id, request.VatRate, request.InvoiceDate, request.DueDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Invoices.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCustomerInvoiceCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/issue")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Issue(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new IssueCustomerInvoiceCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelCustomerInvoiceCommand(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Record BD export-reporting details (Form-EXP #, LC #, shipment date) for EPB Form-N.
    /// Allowed at any post-Draft, non-Cancelled state.
    /// </summary>
    [HttpPost("{id:long}/mark-exported")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> MarkExported(long id, [FromBody] MarkExportedRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkInvoiceAsExportedCommand(
            id,
            request.EpbFormNumber, request.LcNumber, request.ShipmentDate,
            request.IncoTerm, request.PortOfLoading, request.PortOfDischarge,
            request.VesselName, request.CountryOfDestination, request.ShippingMarks,
            request.TotalPackages, request.GrossWeightKg, request.NetWeightKg,
            request.ContainerNumber, request.SealNumber, request.TruckNumber), ct);
        return Ok(result);
    }

    /// <summary>Bulk-set per-line packing breakdown (carton numbers, units/carton, weights).</summary>
    [HttpPost("{id:long}/set-lines-packing")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<IActionResult> SetLinesPacking(long id, [FromBody] SetLinesPackingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetInvoiceLinesPackingCommand(id, request.Lines), ct);
        return Ok(result);
    }
}

public record SetLinesPackingRequest(IReadOnlyList<InvoiceLinePackingInput> Lines);

public record MarkExportedRequest(
    string? EpbFormNumber,
    string? LcNumber,
    DateOnly? ShipmentDate,
    string? IncoTerm,
    string? PortOfLoading,
    string? PortOfDischarge,
    string? VesselName,
    string? CountryOfDestination,
    string? ShippingMarks,
    int? TotalPackages,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    string? ContainerNumber,
    string? SealNumber,
    string? TruckNumber);

public record CreateCustomerInvoiceRequest(
    long SalesOrderId,
    decimal VatRate,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string? Notes,
    IReadOnlyList<CustomerInvoiceLineInput> Lines);

public record UpdateCustomerInvoiceRequest(
    decimal VatRate,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<CustomerInvoiceLineInput> Lines);
