using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payment.Commands;
using BengalTex.ERP.Application.Payment.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] long? supplierInvoiceId = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] string? paymentMethod = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetPaymentsQuery(parameters, supplierInvoiceId, supplierId, paymentMethod), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPaymentByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Payments.Create)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePaymentCommand(
            request.SupplierInvoiceId, request.PaymentDate, request.Amount,
            request.PaymentMethod, request.ReferenceNumber, request.Notes, request.ExchangeRate,
            request.AitAmount, request.VdsAmount
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Payments.Edit)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePaymentCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Phase A5b — data for the printable AIT/VDS withholding certificate issued to the supplier.</summary>
    [HttpGet("{id:long}/withholding-certificate")]
    [HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> WithholdingCertificate(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetWithholdingCertificateQuery(id), ct));
}

public record CreatePaymentRequest(
    long SupplierInvoiceId,
    DateOnly PaymentDate,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    string? Notes,
    decimal? ExchangeRate = null,
    decimal AitAmount = 0m,
    decimal VdsAmount = 0m);
