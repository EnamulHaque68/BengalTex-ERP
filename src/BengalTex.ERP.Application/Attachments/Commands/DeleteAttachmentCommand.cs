using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Attachments.Commands;

public sealed record DeleteAttachmentCommand(long Id) : IRequest<ApiResponse<bool>>;

internal sealed class DeleteAttachmentCommandHandler
    : IRequestHandler<DeleteAttachmentCommand, ApiResponse<bool>>
{
    private readonly IAttachmentService _service;

    public DeleteAttachmentCommandHandler(IAttachmentService service) => _service = service;

    public async Task<ApiResponse<bool>> Handle(
        DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(request.Id, cancellationToken);
        return deleted
            ? ApiResponse<bool>.Ok(true, "Attachment deleted.")
            : ApiResponse<bool>.Fail("Attachment not found.");
    }
}
