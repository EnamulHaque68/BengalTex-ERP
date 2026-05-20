using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.QuarantineDisposition.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QuarantineDisposition.Queries;

public sealed record GetQuarantineDispositionsQuery(
    PagedQueryParameters Parameters,
    string? DispositionType = null,
    int? QuarantineWarehouseId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<QuarantineDispositionListItemDto>>>;

internal sealed class GetQuarantineDispositionsQueryHandler
    : IRequestHandler<GetQuarantineDispositionsQuery, ApiResponse<PagedResult<QuarantineDispositionListItemDto>>>
{
    private readonly IRepository<Domain.Entities.QuarantineDisposition, long> _repo;

    public GetQuarantineDispositionsQueryHandler(IRepository<Domain.Entities.QuarantineDisposition, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<PagedResult<QuarantineDispositionListItemDto>>> Handle(
        GetQuarantineDispositionsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!string.IsNullOrEmpty(request.DispositionType)
            && Enum.TryParse<Domain.Entities.DispositionType>(request.DispositionType, out var dt))
            query = query.Where(d => d.DispositionType == dt);

        if (request.QuarantineWarehouseId.HasValue)
            query = query.Where(d => d.QuarantineWarehouseId == request.QuarantineWarehouseId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.QuarantineDispositionStatus>(request.Status, out var st))
            query = query.Where(d => d.Status == st);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d =>
                d.Code.Contains(search) ||
                d.QuarantineWarehouse.Name.Contains(search) ||
                (d.Reason != null && d.Reason.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")            => query.OrderByDescending(d => d.Code),
            ("code", _)                 => query.OrderBy(d => d.Code),
            ("dispositiondate", "asc")  => query.OrderBy(d => d.DispositionDate),
            ("dispositiondate", _)      => query.OrderByDescending(d => d.DispositionDate),
            _                           => query.OrderByDescending(d => d.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(d => new QuarantineDispositionListItemDto(
                d.Id, d.Code,
                d.DispositionType.ToString(),
                d.DispositionDate,
                d.QuarantineWarehouseId, d.QuarantineWarehouse.Name,
                d.DestinationWarehouse != null ? d.DestinationWarehouse.Name : null,
                d.Status.ToString(),
                d.Lines.Count,
                d.Lines.Sum(l => l.Quantity)))
            .ToListAsync(cancellationToken);

        var result = PagedResult<QuarantineDispositionListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<QuarantineDispositionListItemDto>>.Ok(result);
    }
}
