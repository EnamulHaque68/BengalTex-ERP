using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.GoodsReceipt.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.GoodsReceipt.Queries;

public sealed record GetGoodsReceiptsQuery(
    PagedQueryParameters Parameters,
    long? PurchaseOrderId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<GoodsReceiptListItemDto>>>;

internal sealed class GetGoodsReceiptsQueryHandler
    : IRequestHandler<GetGoodsReceiptsQuery, ApiResponse<PagedResult<GoodsReceiptListItemDto>>>
{
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _repo;

    public GetGoodsReceiptsQueryHandler(IRepository<Domain.Entities.GoodsReceiptNote, long> repo) =>
        _repo = repo;

    public async Task<ApiResponse<PagedResult<GoodsReceiptListItemDto>>> Handle(
        GetGoodsReceiptsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.PurchaseOrderId.HasValue)
            query = query.Where(g => g.PurchaseOrderId == request.PurchaseOrderId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.GoodsReceiptStatus>(request.Status, out var status))
        {
            query = query.Where(g => g.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(g =>
                g.Code.Contains(search) ||
                g.PurchaseOrder.Code.Contains(search) ||
                g.PurchaseOrder.Supplier.Name.Contains(search) ||
                (g.SupplierDeliveryRef != null && g.SupplierDeliveryRef.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")     => query.OrderByDescending(g => g.Code),
            ("code", _)          => query.OrderBy(g => g.Code),
            ("receivedate", "asc") => query.OrderBy(g => g.ReceiveDate),
            ("receivedate", _)   => query.OrderByDescending(g => g.ReceiveDate),
            ("status", "desc")   => query.OrderByDescending(g => g.Status),
            ("status", _)        => query.OrderBy(g => g.Status),
            _                    => query.OrderByDescending(g => g.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(g => new GoodsReceiptListItemDto(
                g.Id, g.Code,
                g.PurchaseOrderId, g.PurchaseOrder.Code,
                g.PurchaseOrder.Supplier.Name,
                g.ReceiveDate,
                g.ReceivingWarehouse.Code,
                g.Status.ToString(),
                g.Lines.Count))
            .ToListAsync(cancellationToken);

        var result = PagedResult<GoodsReceiptListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<GoodsReceiptListItemDto>>.Ok(result);
    }
}
