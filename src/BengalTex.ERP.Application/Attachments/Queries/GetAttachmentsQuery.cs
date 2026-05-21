using BengalTex.ERP.Application.Attachments.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Attachments.Queries;

public sealed record GetAttachmentsQuery(string EntityType, long EntityId)
    : IRequest<ApiResponse<IReadOnlyList<AttachmentDto>>>;

internal sealed class GetAttachmentsQueryHandler
    : IRequestHandler<GetAttachmentsQuery, ApiResponse<IReadOnlyList<AttachmentDto>>>
{
    private readonly IAttachmentService _service;

    public GetAttachmentsQueryHandler(IAttachmentService service) => _service = service;

    public async Task<ApiResponse<IReadOnlyList<AttachmentDto>>> Handle(
        GetAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var items = await _service.GetForEntityAsync(request.EntityType, request.EntityId, cancellationToken);
        return ApiResponse<IReadOnlyList<AttachmentDto>>.Ok(items);
    }
}
