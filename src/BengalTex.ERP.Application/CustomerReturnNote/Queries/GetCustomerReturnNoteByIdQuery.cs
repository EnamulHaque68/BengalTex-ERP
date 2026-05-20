using BengalTex.ERP.Application.CustomerReturnNote.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerReturnNote.Queries;

public sealed record GetCustomerReturnNoteByIdQuery(long Id) : IRequest<ApiResponse<CustomerReturnNoteDto>>;

internal sealed class GetCustomerReturnNoteByIdQueryHandler
    : IRequestHandler<GetCustomerReturnNoteByIdQuery, ApiResponse<CustomerReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.CustomerReturnNote, long> _repo;

    public GetCustomerReturnNoteByIdQueryHandler(IRepository<Domain.Entities.CustomerReturnNote, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<CustomerReturnNoteDto>> Handle(
        GetCustomerReturnNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var crn = await _repo.Query()
            .AsNoTracking()
            .Include(c => c.DeliveryNote).ThenInclude(d => d.SalesOrder).ThenInclude(s => s.Customer)
            .Include(c => c.ReturnWarehouse)
            .Include(c => c.Lines).ThenInclude(l => l.DeliveryNoteLine)
            .Include(c => c.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (crn is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Customer return note not found.");

        var lines = crn.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l =>
            {
                // "PreviouslyReturned" for a Draft CRN excludes this CRN's own qty (so the available
                // figure stays accurate while editing). For Posted CRNs, DeliveryNoteLine.ReturnedQuantity
                // already includes this CRN's contribution, so subtract it back out.
                var includesSelf = crn.Status == Domain.Entities.CustomerReturnNoteStatus.Posted;
                var previouslyReturned = includesSelf
                    ? l.DeliveryNoteLine.ReturnedQuantity - l.ReturnedQuantity
                    : l.DeliveryNoteLine.ReturnedQuantity;

                return new CustomerReturnNoteLineDto(
                    l.Id,
                    l.DeliveryNoteLineId,
                    l.ProductId,
                    l.Product.Code,
                    l.Product.Name,
                    l.Product.UnitOfMeasure.Code,
                    l.DeliveryNoteLine.DispatchedQuantity,
                    previouslyReturned,
                    l.ReturnedQuantity,
                    l.DeliveryNoteLine.DispatchedQuantity - previouslyReturned,
                    l.SortOrder,
                    l.LineNotes);
            })
            .ToList();

        var dto = new CustomerReturnNoteDto(
            crn.Id, crn.Code,
            crn.DeliveryNoteId, crn.DeliveryNote.Code,
            crn.DeliveryNote.SalesOrderId, crn.DeliveryNote.SalesOrder.Code,
            crn.DeliveryNote.SalesOrder.CustomerId,
            crn.DeliveryNote.SalesOrder.Customer.Name,
            crn.ReturnDate,
            crn.ReturnWarehouseId, crn.ReturnWarehouse.Code, crn.ReturnWarehouse.Name,
            crn.Status.ToString(),
            crn.VehicleNumber, crn.Reason,
            crn.PostedAt, crn.PostedBy, crn.Notes,
            lines);

        return ApiResponse<CustomerReturnNoteDto>.Ok(dto);
    }
}
