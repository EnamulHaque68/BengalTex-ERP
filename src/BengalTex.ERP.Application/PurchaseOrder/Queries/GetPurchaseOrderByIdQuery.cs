using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.PurchaseOrder.Queries;

public sealed record GetPurchaseOrderByIdQuery(long Id) : IRequest<ApiResponse<PurchaseOrderDto>>;

internal sealed class GetPurchaseOrderByIdQueryHandler
    : IRequestHandler<GetPurchaseOrderByIdQuery, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;

    public GetPurchaseOrderByIdQueryHandler(IRepository<Domain.Entities.PurchaseOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.DeliveryWarehouse)
            .Include(p => p.Currency)
            .Include(p => p.PurchaseRequisition)
            .Include(p => p.SupplierQuotation)
            .Include(p => p.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (po is null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");

        var lines = po.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new PurchaseOrderLineDto(
                l.Id, l.RawMaterialId, l.RawMaterial.Code, l.RawMaterial.Name,
                l.RawMaterial.UnitOfMeasure.Code,
                l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                l.ReceivedQuantity, l.SortOrder, l.LineNotes))
            .ToList();

        var totalAmount = lines.Sum(l => l.LineTotal);

        var dto = new PurchaseOrderDto(
            po.Id, po.Code, po.SupplierId, po.Supplier.Code, po.Supplier.Name,
            po.OrderDate, po.ExpectedDeliveryDate,
            po.DeliveryWarehouseId, po.DeliveryWarehouse?.Name,
            po.Status.ToString(),
            po.CurrencyId, po.Currency.Code, po.Currency.Symbol, po.ExchangeRate,
            po.ApprovedAt, po.ApprovedBy, po.Notes,
            totalAmount, totalAmount * po.ExchangeRate, lines,
            po.PurchaseRequisitionId, po.PurchaseRequisition != null ? po.PurchaseRequisition.Code : null,
            po.SupplierQuotationId, po.SupplierQuotation != null ? po.SupplierQuotation.Code : null);

        return ApiResponse<PurchaseOrderDto>.Ok(dto);
    }
}
