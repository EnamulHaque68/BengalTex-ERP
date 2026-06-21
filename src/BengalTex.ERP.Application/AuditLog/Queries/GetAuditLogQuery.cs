using BengalTex.ERP.Application.AuditLog.Dtos;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.AuditLog.Queries;

public sealed record GetAuditLogQuery(
    PagedQueryParameters Parameters,
    string? EntityType = null,
    string? Action = null,
    string? UserName = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? EntityKey = null
) : IRequest<ApiResponse<PagedResult<AuditLogEntryDto>>>;

internal sealed class GetAuditLogQueryHandler
    : IRequestHandler<GetAuditLogQuery, ApiResponse<PagedResult<AuditLogEntryDto>>>
{
    private readonly IAuditLogQueryService _service;

    public GetAuditLogQueryHandler(IAuditLogQueryService service) => _service = service;

    public async Task<ApiResponse<PagedResult<AuditLogEntryDto>>> Handle(
        GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.QueryAsync(
            request.Parameters, request.EntityType, request.Action,
            request.UserName, request.FromDate, request.ToDate, request.EntityKey, cancellationToken);
        return ApiResponse<PagedResult<AuditLogEntryDto>>.Ok(result);
    }
}

public sealed record GetAuditLogEntityTypesQuery() : IRequest<ApiResponse<IReadOnlyList<string>>>;

internal sealed class GetAuditLogEntityTypesQueryHandler
    : IRequestHandler<GetAuditLogEntityTypesQuery, ApiResponse<IReadOnlyList<string>>>
{
    private readonly IAuditLogQueryService _service;

    public GetAuditLogEntityTypesQueryHandler(IAuditLogQueryService service) => _service = service;

    public async Task<ApiResponse<IReadOnlyList<string>>> Handle(
        GetAuditLogEntityTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await _service.GetEntityTypesAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<string>>.Ok(types);
    }
}
