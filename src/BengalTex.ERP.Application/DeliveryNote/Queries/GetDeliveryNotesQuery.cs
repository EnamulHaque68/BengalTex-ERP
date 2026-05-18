using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.DeliveryNote.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.DeliveryNote.Queries;

public sealed record GetDeliveryNotesQuery(
    PagedQueryParameters Parameters,
    long? SalesOrderId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<DeliveryNoteListItemDto>>>;

internal sealed class GetDeliveryNotesQueryHandler
    : IRequestHandler<GetDeliveryNotesQuery, ApiResponse<PagedResult<DeliveryNoteListItemDto>>>
{
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _repo;

    public GetDeliveryNotesQueryHandler(IRepository<Domain.Entities.DeliveryNote, long> repo) =>
        _repo = repo;

    public async Task<ApiResponse<PagedResult<DeliveryNoteListItemDto>>> Handle(
        GetDeliveryNotesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.SalesOrderId.HasValue)
            query = query.Where(d => d.SalesOrderId == request.SalesOrderId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.DeliveryNoteStatus>(request.Status, out var status))
        {
            query = query.Where(d => d.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d =>
                d.Code.Contains(search) ||
                d.SalesOrder.Code.Contains(search) ||
                d.SalesOrder.Customer.Name.Contains(search) ||
                (d.VehicleNumber != null && d.VehicleNumber.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(d => d.Code),
            ("code", _)             => query.OrderBy(d => d.Code),
            ("dispatchdate", "asc") => query.OrderBy(d => d.DispatchDate),
            ("dispatchdate", _)     => query.OrderByDescending(d => d.DispatchDate),
            ("status", "desc")      => query.OrderByDescending(d => d.Status),
            ("status", _)           => query.OrderBy(d => d.Status),
            _                       => query.OrderByDescending(d => d.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(d => new DeliveryNoteListItemDto(
                d.Id, d.Code,
                d.SalesOrderId, d.SalesOrder.Code,
                d.SalesOrder.Customer.Name,
                d.DispatchDate,
                d.DispatchWarehouse.Code,
                d.Status.ToString(),
                d.Lines.Count))
            .ToListAsync(cancellationToken);

        var result = PagedResult<DeliveryNoteListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<DeliveryNoteListItemDto>>.Ok(result);
    }
}
