using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Attachments.Commands;
using BengalTex.ERP.Application.Attachments.Queries;
using BengalTex.ERP.Shared.Common;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/attachments")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;   // 25 MB

    private readonly IMediator _mediator;

    public AttachmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Upload a file and attach it to (entityType, entityId).</summary>
    [HttpPost]
    [HasPermission(Permissions.Attachments.Manage)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload([FromForm] UploadAttachmentRequest request, CancellationToken ct)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No file was uploaded."));
        if (string.IsNullOrWhiteSpace(request.EntityType) || request.EntityId <= 0)
            return BadRequest(ApiResponse<string>.Fail("entityType and a positive entityId are required."));

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadAttachmentCommand(
            stream, file.FileName, file.ContentType, request.EntityType, request.EntityId,
            request.Description, request.Category), ct);

        return Ok(result);
    }

    /// <summary>List attachments for a given entity.</summary>
    [HttpGet]
    [HasPermission(Permissions.Attachments.View)]
    public async Task<IActionResult> GetForEntity(
        [FromQuery] string entityType, [FromQuery] long entityId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAttachmentsQuery(entityType, entityId), ct);
        return Ok(result);
    }

    /// <summary>Download an attachment's bytes.</summary>
    [HttpGet("{id:long}/download")]
    [HasPermission(Permissions.Attachments.View)]
    public async Task<IActionResult> Download(long id, CancellationToken ct)
    {
        var download = await _mediator.Send(new DownloadAttachmentQuery(id), ct);
        if (download is null) return NotFound();
        return File(download.Content, download.ContentType, download.FileName);
    }

    /// <summary>Delete an attachment (DB row + stored file).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Attachments.Manage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAttachmentCommand(id), ct);
        return Ok(result);
    }
}

/// <summary>
/// Multipart upload form. Bound as a single complex type (not separate [FromForm]
/// scalars) so Swashbuckle can generate the multipart/form-data request schema.
/// </summary>
public sealed class UploadAttachmentRequest
{
    public IFormFile? File { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
}
