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
    private readonly IRepository<Domain.Entities.StockReservation, long> _reservationRepo;

    public GetProductionOrdersQueryHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.StockReservation, long> reservationRepo)
    {
        _repo = repo;
        _reservationRepo = reservationRepo;
    }

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
                p.SalesOrder != null ? p.SalesOrder.Code : null,
                p.RequiresQc,
                false,   // QcHeld — patched below from the live QcHold reservation
                0m))     // QcHeldQuantity
            .ToListAsync(cancellationToken);

        // Phase 5 (QC-hold upgrade): remaining held qty = Active "QcHold" reservation per production.
        var qcCandidateIds = items.Where(i => i.RequiresQc).Select(i => i.Id).ToList();
        if (qcCandidateIds.Count > 0)
        {
            var heldRows = await _reservationRepo.Query()
                .Where(r => r.ReferenceType == "QcHold"
                    && r.Status == Domain.Entities.ReservationStatus.Active
                    && qcCandidateIds.Contains(r.ReferenceId))
                .Select(r => new { r.ReferenceId, r.Quantity })
                .ToListAsync(cancellationToken);

            var heldByPo = heldRows.GroupBy(x => x.ReferenceId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            for (var i = 0; i < items.Count; i++)
            {
                if (heldByPo.TryGetValue(items[i].Id, out var qty) && qty > 0m)
                    items[i] = items[i] with { QcHeld = true, QcHeldQuantity = qty };
            }
        }

        var result = PagedResult<ProductionOrderListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<ProductionOrderListItemDto>>.Ok(result);
    }
}
