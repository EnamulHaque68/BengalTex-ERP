using BengalTex.ERP.Application.Attachments.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Attachments.Commands;

public sealed record UploadAttachmentCommand(
    Stream Content,
    string FileName,
    string ContentType,
    string EntityType,
    long EntityId,
    string? Description,
    string? Category
) : IRequest<ApiResponse<AttachmentDto>>;

internal sealed class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, ApiResponse<AttachmentDto>>
{
    private readonly IAttachmentService _service;

    public UploadAttachmentCommandHandler(IAttachmentService service) => _service = service;

    public async Task<ApiResponse<AttachmentDto>> Handle(
        UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        var dto = await _service.UploadAsync(
            request.Content, request.FileName, request.ContentType,
            request.EntityType, request.EntityId,
            request.Description, request.Category, cancellationToken);

        return ApiResponse<AttachmentDto>.Ok(dto, "File uploaded.");
    }
}
