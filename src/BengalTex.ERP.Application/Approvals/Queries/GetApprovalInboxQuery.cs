using BengalTex.ERP.Application.Approvals.Dtos;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Approvals.Queries;

/// <summary>Pending requests the current user can act on (based on their roles).</summary>
public sealed record GetApprovalInboxQuery() : IRequest<ApiResponse<IReadOnlyList<ApprovalRequestDto>>>;

internal sealed class GetApprovalInboxQueryHandler
    : IRequestHandler<GetApprovalInboxQuery, ApiResponse<IReadOnlyList<ApprovalRequestDto>>>
{
    private readonly IApprovalService _service;
    private readonly ICurrentUserService _currentUser;

    public GetApprovalInboxQueryHandler(IApprovalService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<IReadOnlyList<ApprovalRequestDto>>> Handle(
        GetApprovalInboxQuery request, CancellationToken cancellationToken)
    {
        var roles = _currentUser.Roles;
        var seeAll = roles.Contains("SuperAdmin");
        var items = await _service.GetInboxAsync(roles, seeAll, cancellationToken);
        return ApiResponse<IReadOnlyList<ApprovalRequestDto>>.Ok(items);
    }
}
