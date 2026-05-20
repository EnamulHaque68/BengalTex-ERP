using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.CustomerReturnNote.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerReturnNote.Queries;

public sealed record GetCustomerReturnNotesQuery(
    PagedQueryParameters Parameters,
    long? DeliveryNoteId = null,
    int? CustomerId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<CustomerReturnNoteListItemDto>>>;

internal sealed class GetCustomerReturnNotesQueryHandler
    : IRequestHandler<GetCustomerReturnNotesQuery, ApiResponse<PagedResult<CustomerReturnNoteListItemDto>>>
{
    private readonly IRepository<Domain.Entities.CustomerReturnNote, long> _repo;

    public GetCustomerReturnNotesQueryHandler(IRepository<Domain.Entities.CustomerReturnNote, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<PagedResult<CustomerReturnNoteListItemDto>>> Handle(
        GetCustomerReturnNotesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.DeliveryNoteId.HasValue)
            query = query.Where(c => c.DeliveryNoteId == request.DeliveryNoteId.Value);
        if (request.CustomerId.HasValue)
            query = query.Where(c => c.DeliveryNote.SalesOrder.CustomerId == request.CustomerId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.CustomerReturnNoteStatus>(request.Status, out var st))
        {
            query = query.Where(c => c.Status == st);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c =>
                c.Code.Contains(search) ||
                c.DeliveryNote.Code.Contains(search) ||
                c.DeliveryNote.SalesOrder.Customer.Name.Contains(search) ||
                (c.Reason != null && c.Reason.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(c => c.Code),
            ("code", _)             => query.OrderBy(c => c.Code),
            ("returndate", "asc")   => query.OrderBy(c => c.ReturnDate),
            ("returndate", _)       => query.OrderByDescending(c => c.ReturnDate),
            _                       => query.OrderByDescending(c => c.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(c => new CustomerReturnNoteListItemDto(
                c.Id, c.Code,
                c.DeliveryNoteId, c.DeliveryNote.Code,
                c.DeliveryNote.SalesOrder.CustomerId,
                c.DeliveryNote.SalesOrder.Customer.Name,
                c.ReturnDate,
                c.ReturnWarehouseId, c.ReturnWarehouse.Name,
                c.Status.ToString(),
                c.Lines.Count,
                c.Lines.Sum(l => l.ReturnedQuantity)))
            .ToListAsync(cancellationToken);

        var result = PagedResult<CustomerReturnNoteListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<CustomerReturnNoteListItemDto>>.Ok(result);
    }
}
