using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.QcInspection.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QcInspection.Queries;

public sealed record GetQcInspectionsQuery(
    PagedQueryParameters Parameters,
    string? SourceType = null,
    string? Status = null,
    string? Result = null
) : IRequest<ApiResponse<PagedResult<QcInspectionListItemDto>>>;

internal sealed class GetQcInspectionsQueryHandler
    : IRequestHandler<GetQcInspectionsQuery, ApiResponse<PagedResult<QcInspectionListItemDto>>>
{
    private readonly IRepository<Domain.Entities.QcInspection, long> _repo;

    public GetQcInspectionsQueryHandler(IRepository<Domain.Entities.QcInspection, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<PagedResult<QcInspectionListItemDto>>> Handle(
        GetQcInspectionsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!string.IsNullOrEmpty(request.SourceType)
            && Enum.TryParse<Domain.Entities.QcSourceType>(request.SourceType, out var src))
            query = query.Where(q => q.SourceType == src);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.QcInspectionStatus>(request.Status, out var st))
            query = query.Where(q => q.Status == st);

        if (!string.IsNullOrEmpty(request.Result)
            && Enum.TryParse<Domain.Entities.QcResult>(request.Result, out var res))
            query = query.Where(q => q.OverallResult == res);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(q =>
                q.Code.Contains(search) ||
                (q.GoodsReceiptNote != null && q.GoodsReceiptNote.Code.Contains(search)) ||
                (q.ProductionOrder != null && q.ProductionOrder.Code.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")            => query.OrderByDescending(q => q.Code),
            ("code", _)                 => query.OrderBy(q => q.Code),
            ("inspectiondate", "asc")   => query.OrderBy(q => q.InspectionDate),
            ("inspectiondate", _)       => query.OrderByDescending(q => q.InspectionDate),
            _                           => query.OrderByDescending(q => q.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(q => new QcInspectionListItemDto(
                q.Id, q.Code,
                q.SourceType.ToString(),
                q.GoodsReceiptNoteId != null ? q.GoodsReceiptNote!.Code : q.ProductionOrder!.Code,
                q.InspectionDate,
                q.Status.ToString(),
                q.OverallResult.ToString(),
                q.Lines.Count,
                q.Lines.Sum(l => l.InspectedQuantity),
                q.Lines.Sum(l => l.RejectedQuantity)))
            .ToListAsync(cancellationToken);

        var result = PagedResult<QcInspectionListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<QcInspectionListItemDto>>.Ok(result);
    }
}
