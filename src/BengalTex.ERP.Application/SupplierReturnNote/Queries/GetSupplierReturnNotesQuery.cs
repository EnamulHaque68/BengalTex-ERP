using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.SupplierReturnNote.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierReturnNote.Queries;

public sealed record GetSupplierReturnNotesQuery(
    PagedQueryParameters Parameters,
    long? GoodsReceiptNoteId = null,
    int? SupplierId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<SupplierReturnNoteListItemDto>>>;

internal sealed class GetSupplierReturnNotesQueryHandler
    : IRequestHandler<GetSupplierReturnNotesQuery, ApiResponse<PagedResult<SupplierReturnNoteListItemDto>>>
{
    private readonly IRepository<Domain.Entities.SupplierReturnNote, long> _repo;

    public GetSupplierReturnNotesQueryHandler(IRepository<Domain.Entities.SupplierReturnNote, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<PagedResult<SupplierReturnNoteListItemDto>>> Handle(
        GetSupplierReturnNotesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.GoodsReceiptNoteId.HasValue)
            query = query.Where(s => s.GoodsReceiptNoteId == request.GoodsReceiptNoteId.Value);
        if (request.SupplierId.HasValue)
            query = query.Where(s => s.GoodsReceiptNote.PurchaseOrder.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.SupplierReturnNoteStatus>(request.Status, out var st))
        {
            query = query.Where(s => s.Status == st);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.GoodsReceiptNote.Code.Contains(search) ||
                s.GoodsReceiptNote.PurchaseOrder.Supplier.Name.Contains(search) ||
                (s.Reason != null && s.Reason.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(s => s.Code),
            ("code", _)             => query.OrderBy(s => s.Code),
            ("returndate", "asc")   => query.OrderBy(s => s.ReturnDate),
            ("returndate", _)       => query.OrderByDescending(s => s.ReturnDate),
            _                       => query.OrderByDescending(s => s.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new SupplierReturnNoteListItemDto(
                s.Id, s.Code,
                s.GoodsReceiptNoteId, s.GoodsReceiptNote.Code,
                s.GoodsReceiptNote.PurchaseOrder.SupplierId,
                s.GoodsReceiptNote.PurchaseOrder.Supplier.Name,
                s.ReturnDate,
                s.ReturnFromWarehouseId, s.ReturnFromWarehouse.Name,
                s.Status.ToString(),
                s.Lines.Count,
                s.Lines.Sum(l => l.ReturnedQuantity)))
            .ToListAsync(cancellationToken);

        var result = PagedResult<SupplierReturnNoteListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<SupplierReturnNoteListItemDto>>.Ok(result);
    }
}
