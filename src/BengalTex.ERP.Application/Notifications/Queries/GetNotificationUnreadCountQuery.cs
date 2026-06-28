using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Notifications.Queries;

/// <summary>
/// Count of notifications created after <paramref name="Since"/> (all when null) — the number
/// shown on the notification-bell badge. "Since" is the moment the user last opened the
/// notifications page (tracked client-side), so this is an "unread since I last looked" count.
/// </summary>
public sealed record GetNotificationUnreadCountQuery(DateTimeOffset? Since) : IRequest<ApiResponse<int>>;

internal sealed class GetNotificationUnreadCountQueryHandler
    : IRequestHandler<GetNotificationUnreadCountQuery, ApiResponse<int>>
{
    private readonly INotificationService _service;

    public GetNotificationUnreadCountQueryHandler(INotificationService service) => _service = service;

    public async Task<ApiResponse<int>> Handle(GetNotificationUnreadCountQuery request, CancellationToken ct)
    {
        var count = await _service.CountSinceAsync(request.Since, ct);
        return ApiResponse<int>.Ok(count);
    }
}
