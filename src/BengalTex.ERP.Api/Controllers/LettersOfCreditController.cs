using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Banking.Commands;
using BengalTex.ERP.Application.Banking.Queries;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/letters-of-credit")]
[Authorize]
public class LettersOfCreditController : ControllerBase
{
    private readonly IMediator _mediator;

    public LettersOfCreditController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Banking.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? supplierId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLettersOfCreditQuery(parameters, supplierId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Banking.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLetterOfCreditByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>The non-cancelled LC linked to a PO (for the goods-receipt auto-suggest); null if none.</summary>
    [HttpGet("for-po/{purchaseOrderId:long}")]
    [HasPermission(Permissions.Banking.View)]
    public async Task<IActionResult> ForPurchaseOrder(long purchaseOrderId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLcForPurchaseOrderQuery(purchaseOrderId), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Banking.Create)]
    public async Task<IActionResult> Create([FromBody] CreateLetterOfCreditRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateLetterOfCreditCommand(
            request.Code, request.LcNumber, request.IssuingBank, request.SupplierId, request.PurchaseOrderId,
            request.CurrencyId, request.ExchangeRate, request.Amount,
            request.IssueDate, request.ExpiryDate, request.TenorDays, request.Notes,
            ParseType(request.Type), request.MasterLcReference, request.MasterLcBuyer), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Banking.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateLetterOfCreditRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLetterOfCreditCommand(
            id, request.LcNumber, request.IssuingBank, request.SupplierId, request.PurchaseOrderId,
            request.CurrencyId, request.ExchangeRate, request.Amount,
            request.IssueDate, request.ExpiryDate, request.TenorDays, request.Notes,
            ParseType(request.Type), request.MasterLcReference, request.MasterLcBuyer), ct);
        return Ok(result);
    }

    /// <summary>
    /// Parse the LC type sent as a string by the client (no global JsonStringEnumConverter is
    /// registered, so an enum-typed body field would fail to bind). Defaults to Import.
    /// </summary>
    private static LcType ParseType(string? type) =>
        Enum.TryParse<LcType>(type, ignoreCase: true, out var parsed) ? parsed : LcType.Import;

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Banking.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteLetterOfCreditCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/open")]
    [HasPermission(Permissions.Banking.Manage)]
    public Task<IActionResult> Open(long id, CancellationToken ct) => Transition(id, "open", null, ct);

    [HttpPost("{id:long}/ship")]
    [HasPermission(Permissions.Banking.Manage)]
    public Task<IActionResult> Ship(long id, [FromBody] LcDateRequest? body, CancellationToken ct) => Transition(id, "ship", body?.Date, ct);

    [HttpPost("{id:long}/settle")]
    [HasPermission(Permissions.Banking.Manage)]
    public Task<IActionResult> Settle(long id, [FromBody] LcDateRequest? body, CancellationToken ct) => Transition(id, "settle", body?.Date, ct);

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Banking.Manage)]
    public Task<IActionResult> Cancel(long id, CancellationToken ct) => Transition(id, "cancel", null, ct);

    private async Task<IActionResult> Transition(long id, string action, DateOnly? date, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeLcStatusCommand(id, action, date), ct);
        return Ok(result);
    }
}

public record CreateLetterOfCreditRequest(
    string? Code,
    string LcNumber,
    string IssuingBank,
    int SupplierId,
    long? PurchaseOrderId,
    int CurrencyId,
    decimal ExchangeRate,
    decimal Amount,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    int TenorDays,
    string? Notes,
    string? Type = "Import",          // "Import" | "BackToBack" — parsed in the controller
    string? MasterLcReference = null,
    string? MasterLcBuyer = null);

public record UpdateLetterOfCreditRequest(
    string LcNumber,
    string IssuingBank,
    int SupplierId,
    long? PurchaseOrderId,
    int CurrencyId,
    decimal ExchangeRate,
    decimal Amount,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    int TenorDays,
    string? Notes,
    string? Type = "Import",          // "Import" | "BackToBack" — parsed in the controller
    string? MasterLcReference = null,
    string? MasterLcBuyer = null);

public record LcDateRequest(DateOnly? Date);
