using BengalTex.ERP.Application.Services;
using MediatR;

namespace BengalTex.ERP.Application.Attachments.Queries;

/// <summary>
/// Returns the file stream + metadata for download (or null if not found).
/// Not wrapped in ApiResponse — the controller turns it into a FileStreamResult.
/// </summary>
public sealed record DownloadAttachmentQuery(long Id) : IRequest<AttachmentDownload?>;

internal sealed class DownloadAttachmentQueryHandler
    : IRequestHandler<DownloadAttachmentQuery, AttachmentDownload?>
{
    private readonly IAttachmentService _service;

    public DownloadAttachmentQueryHandler(IAttachmentService service) => _service = service;

    public Task<AttachmentDownload?> Handle(DownloadAttachmentQuery request, CancellationToken cancellationToken)
        => _service.OpenAsync(request.Id, cancellationToken);
}
