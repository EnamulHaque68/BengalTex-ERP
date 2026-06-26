using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Queries;

public sealed record GetProductionOrdersQuery(
    PagedQueryParameters Parameters,
    int? ProductId = null,
    string? Status = null,
    long? SalesOrderId = null
) : IRequest<ApiResponse<PagedResult<ProductionOrderListItemDto>>>;

internal sealed class GetProductionOrdersQueryHandler
    : IRequestHandler<GetProductionOrdersQuery, ApiResponse<PagedResult<ProductionOrderListItemDto>>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;

    public GetProductionOrdersQueryHandler(IRepository<Domain.Entities.ProductionOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<ProductionOrderListItemDto>>> Handle(
        GetProductionOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.ProductId.HasValue)
            query = query.Where(p => p.ProductId == request.ProductId.Value);

        if (request.SalesOrderId.HasValue)
            query = query.Where(p => p.SalesOrderId == request.SalesOrderId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.ProductionOrderStatus>(request.Status, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Code.Contains(search) ||
                p.Product.Code.Contains(search) ||
                p.Product.Name.Contains(search));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")    => query.OrderByDescending(p => p.Code),
            ("code", _)         => query.OrderBy(p => p.Code),
            ("product", "desc") => query.OrderByDescending(p => p.Product.Name),
            ("product", _)      => query.OrderBy(p => p.Product.Name),
            ("status", "desc")  => query.OrderByDescending(p => p.Status),
            ("status", _)       => query.OrderBy(p => p.Status),
            _                   => query.OrderByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(p => new ProductionOrderListItemDto(
                p.Id, p.Code,
                p.ProductId, p.Product.Name,
                p.Bom.Version,
                p.Quantity,
                p.Status.ToString(),
                p.PlannedStartDate,
                p.ActualEndDate,
                p.Stages.Count,
                p.Stages.Count(s => s.Status == Domain.Entities.ProductionStageStatus.Completed
                                 || s.Status == Domain.Entities.ProductionStageStatus.Skipped),
                p.Stages
                    .Where(s => s.Status != Domain.Entities.ProductionStageStatus.Completed
                             && s.Status != Domain.Entities.ProductionStageStatus.Skipped)
                    .OrderBy(s => s.Sequence)
                    .Select(s => s.StageName)
                    .FirstOrDefault(),
                p.SalesOrderId,
                p.SalesOrder != null ? p.SalesOrder.Code : null))
            .ToListAsync(cancellationToken);

        var result = PagedResult<ProductionOrderListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<ProductionOrderListItemDto>>.Ok(result);
    }
}
