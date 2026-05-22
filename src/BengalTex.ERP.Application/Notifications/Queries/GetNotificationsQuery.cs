using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Notifications.Queries;

/// <summary>Paged notification log, optionally filtered by channel and/or status.</summary>
public sealed record GetNotificationsQuery(
    PagedQueryParameters Parameters,
    string? Channel = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<NotificationDto>>>;

internal sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, ApiResponse<PagedResult<NotificationDto>>>
{
    private readonly INotificationService _service;

    public GetNotificationsQueryHandler(INotificationService service) => _service = service;

    public async Task<ApiResponse<PagedResult<NotificationDto>>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.QueryAsync(
            request.Parameters, request.Channel, request.Status, cancellationToken);
        return ApiResponse<PagedResult<NotificationDto>>.Ok(result);
    }
}
