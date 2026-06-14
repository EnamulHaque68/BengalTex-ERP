using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Receipt.Commands;
using BengalTex.ERP.Application.Receipt.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/receipts")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReceiptsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] long? customerInvoiceId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? paymentMethod = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetReceiptsQuery(parameters, customerInvoiceId, customerId, paymentMethod), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReceiptByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Payments.Create)]
    public async Task<IActionResult> Create([FromBody] CreateReceiptRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateReceiptCommand(
            request.CustomerInvoiceId, request.ReceiptDate, request.Amount,
            request.PaymentMethod, request.ReferenceNumber, request.Notes, request.ExchangeRate
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Payments.Edit)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteReceiptCommand(id), ct);
        return Ok(result);
    }
}

public record CreateReceiptRequest(
    long CustomerInvoiceId,
    DateOnly ReceiptDate,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    string? Notes,
    decimal? ExchangeRate = null);
