using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.ProformaInvoices.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/proforma-invoices")]
[Authorize]
public class ProformaInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorage _files;
    public ProformaInvoicesController(IMediator mediator, IFileStorage files)
    { _mediator = mediator; _files = files; }

    [HttpGet]
    [HasPermission(Permissions.ProformaInvoices.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProformaInvoicesQuery(parameters, status, customerId), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ProformaInvoices.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProformaInvoiceByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.ProformaInvoices.Create)]
    public async Task<IActionResult> Create([FromBody] CreateProformaInvoiceCommand cmd, CancellationToken ct)
        => Ok(await _mediator.Send(cmd, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.ProformaInvoices.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProformaInvoiceBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateProformaInvoiceCommand(
            id, body.IssueDate, body.ValidUntil, body.VatRate, body.Notes, body.Lines), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.ProformaInvoices.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteProformaInvoiceCommand(id), ct));

    [HttpPost("{id:long}/send")]
    [HasPermission(Permissions.ProformaInvoices.Send)]
    public async Task<IActionResult> Send(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new SendProformaInvoiceCommand(id), ct));

    [HttpPost("{id:long}/accept")]
    [HasPermission(Permissions.ProformaInvoices.Send)]
    public async Task<IActionResult> Accept(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new AcceptProformaInvoiceCommand(id), ct));

    [HttpPost("{id:long}/expire")]
    [HasPermission(Permissions.ProformaInvoices.Send)]
    public async Task<IActionResult> Expire(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new ExpireProformaInvoiceCommand(id), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.ProformaInvoices.Send)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelProformaInvoiceCommand(id), ct));

    [HttpPost("{id:long}/convert")]
    [HasPermission(Permissions.ProformaInvoices.Convert)]
    public async Task<IActionResult> Convert(long id, [FromBody] ConvertProformaBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new ConvertProformaToCustomerInvoiceCommand(
            id, body.SalesOrderId, body.InvoiceDate, body.DueDate), ct));

    /// <summary>Converts an accepted proforma (from a quotation) into a Sales Order, recording the customer confirmation.</summary>
    [HttpPost("{id:long}/convert-to-sales-order")]
    [HasPermission(Permissions.ProformaInvoices.Convert)]
    public async Task<IActionResult> ConvertToSalesOrder(long id, [FromBody] ConvertProformaToSoBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new ConvertProformaToSalesOrderCommand(
            id, body.CustomerConfirmationType, body.CustomerConfirmationReference,
            body.CustomerConfirmationDate, body.CustomerConfirmationAttachment), ct));

    /// <summary>Uploads a customer-confirmation document (PO/LC/signed PI/email). Returns the storage path to pass to convert.</summary>
    [HttpPost("{id:long}/confirmation-attachment")]
    [HasPermission(Permissions.ProformaInvoices.Convert)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadConfirmation(long id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");
        await using var stream = file.OpenReadStream();
        var stored = await _files.SaveAsync(stream, file.FileName, file.ContentType, "ProformaConfirmation", ct);
        return Ok(new { storagePath = stored.StoragePath });
    }

    /// <summary>Streams the customer-confirmation attachment for audit.</summary>
    [HttpGet("{id:long}/confirmation-attachment")]
    [HasPermission(Permissions.ProformaInvoices.View)]
    public async Task<IActionResult> GetConfirmation(long id, CancellationToken ct)
    {
        var path = await _mediator.Send(new GetProformaConfirmationAttachmentPathQuery(id), ct);
        if (string.IsNullOrEmpty(path) || !await _files.ExistsAsync(path, ct)) return NotFound();
        var stream = await _files.OpenReadAsync(path, ct);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch { ".pdf" => "application/pdf", ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", _ => "application/octet-stream" };
        return File(stream, contentType);
    }
}

public record UpdateProformaInvoiceBody(
    DateOnly IssueDate, DateOnly ValidUntil, decimal VatRate, string? Notes,
    IReadOnlyList<ProformaInvoiceLineInput> Lines);

public record ConvertProformaBody(long SalesOrderId, DateOnly InvoiceDate, DateOnly? DueDate);

public record ConvertProformaToSoBody(
    string CustomerConfirmationType,
    string? CustomerConfirmationReference,
    DateOnly? CustomerConfirmationDate,
    string? CustomerConfirmationAttachment);
