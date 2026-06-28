using BengalTex.ERP.Application.DeliveryNote.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.DeliveryNote.Queries;

public sealed record GetDeliveryNoteByIdQuery(long Id) : IRequest<ApiResponse<DeliveryNoteDto>>;

internal sealed class GetDeliveryNoteByIdQueryHandler
    : IRequestHandler<GetDeliveryNoteByIdQuery, ApiResponse<DeliveryNoteDto>>
{
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _repo;

    public GetDeliveryNoteByIdQueryHandler(IRepository<Domain.Entities.DeliveryNote, long> repo) =>
        _repo = repo;

    public async Task<ApiResponse<DeliveryNoteDto>> Handle(
        GetDeliveryNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var dn = await _repo.Query()
            .AsNoTracking()
            .Include(d => d.SalesOrder).ThenInclude(s => s.Customer)
            .Include(d => d.DispatchWarehouse)
            .Include(d => d.Lines)
                .ThenInclude(l => l.SalesOrderLine)
                .ThenInclude(sl => sl.Product)
                .ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (dn is null) return ApiResponse<DeliveryNoteDto>.Fail("Delivery note not found.");

        var lines = dn.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new DeliveryNoteLineDto(
                l.Id, l.SalesOrderLineId,
                l.SalesOrderLine.ProductId,
                l.SalesOrderLine.Product.Code,
                l.SalesOrderLine.Product.Name,
                l.SalesOrderLine.Product.UnitOfMeasure.Code,
                l.SalesOrderLine.Quantity,
                l.DispatchedQuantity,
                l.ReturnedQuantity,
                l.InvoicedQuantity,
                l.DispatchedQuantity - l.InvoicedQuantity,
                l.SortOrder, l.LineNotes))
            .ToList();

        var dto = new DeliveryNoteDto(
            dn.Id, dn.Code,
            dn.SalesOrderId, dn.SalesOrder.Code,
            dn.SalesOrder.CustomerId, dn.SalesOrder.Customer.Name,
            dn.DispatchDate,
            dn.DispatchWarehouseId, dn.DispatchWarehouse.Name,
            dn.Status.ToString(),
            dn.PlannedDeliveryDate,
            dn.VehicleNumber, dn.TransportCompany, dn.DriverName, dn.DriverContact, dn.DeliveryAddress,
            dn.PostedAt, dn.PostedBy, dn.Notes,
            lines);

        return ApiResponse<DeliveryNoteDto>.Ok(dto);
    }
}
