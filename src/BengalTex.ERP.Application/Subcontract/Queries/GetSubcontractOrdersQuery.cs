using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Subcontract.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Subcontract.Queries;

public sealed record GetSubcontractOrdersQuery(
    PagedQueryParameters Parameters,
    int? SubcontractorId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<SubcontractOrderListItemDto>>>;

internal sealed class GetSubcontractOrdersQueryHandler
    : IRequestHandler<GetSubcontractOrdersQuery, ApiResponse<PagedResult<SubcontractOrderListItemDto>>>
{
    private readonly IRepository<SubcontractOrder, long> _repo;

    public GetSubcontractOrdersQueryHandler(IRepository<SubcontractOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<SubcontractOrderListItemDto>>> Handle(
        GetSubcontractOrdersQuery request, CancellationToken ct)
    {
        var query = _repo.Query();

        if (request.SubcontractorId.HasValue)
            query = query.Where(s => s.SubcontractorId == request.SubcontractorId.Value);
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<SubcontractStatus>(request.Status, out var status))
        {
            query = query.Where(s => s.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.Subcontractor.Name.Contains(search) ||
                s.ProcessType.Contains(search));
        }

        query = query.OrderByDescending(s => s.Id);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new SubcontractOrderListItemDto(
                s.Id, s.Code, s.Subcontractor.Name, s.OrderDate, s.ProcessType,
                s.Warehouse.Name, s.Status.ToString(), s.Lines.Count, s.ChargeAmount))
            .ToListAsync(ct);

        var result = PagedResult<SubcontractOrderListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<SubcontractOrderListItemDto>>.Ok(result);
    }
}
