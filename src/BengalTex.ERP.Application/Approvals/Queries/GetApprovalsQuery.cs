using BengalTex.ERP.Application.Approvals.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Approvals.Queries;

/// <summary>All approval requests, optionally filtered by status.</summary>
public sealed record GetApprovalsQuery(string? Status = null)
    : IRequest<ApiResponse<IReadOnlyList<ApprovalRequestDto>>>;

internal sealed class GetApprovalsQueryHandler
    : IRequestHandler<GetApprovalsQuery, ApiResponse<IReadOnlyList<ApprovalRequestDto>>>
{
    private readonly IApprovalService _service;

    public GetApprovalsQueryHandler(IApprovalService service) => _service = service;

    public async Task<ApiResponse<IReadOnlyList<ApprovalRequestDto>>> Handle(
        GetApprovalsQuery request, CancellationToken cancellationToken)
    {
        var items = await _service.GetAllAsync(request.Status, cancellationToken);
        return ApiResponse<IReadOnlyList<ApprovalRequestDto>>.Ok(items);
    }
}

/// <summary>A single approval request with its step history.</summary>
public sealed record GetApprovalByIdQuery(long Id) : IRequest<ApiResponse<ApprovalRequestDto>>;

internal sealed class GetApprovalByIdQueryHandler
    : IRequestHandler<GetApprovalByIdQuery, ApiResponse<ApprovalRequestDto>>
{
    private readonly IApprovalService _service;

    public GetApprovalByIdQueryHandler(IApprovalService service) => _service = service;

    public async Task<ApiResponse<ApprovalRequestDto>> Handle(
        GetApprovalByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _service.GetByIdAsync(request.Id, cancellationToken);
        return dto is null
            ? ApiResponse<ApprovalRequestDto>.Fail("Approval request not found.")
            : ApiResponse<ApprovalRequestDto>.Ok(dto);
    }
}
