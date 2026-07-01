using BengalTex.ERP.Application.GoodsReceipt.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.GoodsReceipt.Queries;

public sealed record GetGoodsReceiptByIdQuery(long Id) : IRequest<ApiResponse<GoodsReceiptDto>>;

internal sealed class GetGoodsReceiptByIdQueryHandler
    : IRequestHandler<GetGoodsReceiptByIdQuery, ApiResponse<GoodsReceiptDto>>
{
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _repo;

    public GetGoodsReceiptByIdQueryHandler(IRepository<Domain.Entities.GoodsReceiptNote, long> repo) =>
        _repo = repo;

    public async Task<ApiResponse<GoodsReceiptDto>> Handle(
        GetGoodsReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        var grn = await _repo.Query()
            .AsNoTracking()
            .Include(g => g.PurchaseOrder).ThenInclude(p => p.Supplier)
            .Include(g => g.ReceivingWarehouse)
            .Include(g => g.LetterOfCredit)
            .Include(g => g.Lines)
                .ThenInclude(l => l.PurchaseOrderLine)
                .ThenInclude(pl => pl.RawMaterial)
                .ThenInclude(rm => rm.UnitOfMeasure)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (grn is null) return ApiResponse<GoodsReceiptDto>.Fail("Goods receipt not found.");

        var lines = grn.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new GoodsReceiptLineDto(
                l.Id, l.PurchaseOrderLineId,
                l.PurchaseOrderLine.RawMaterialId,
                l.PurchaseOrderLine.RawMaterial.Code,
                l.PurchaseOrderLine.RawMaterial.Name,
                l.PurchaseOrderLine.RawMaterial.UnitOfMeasure.Code,
                l.PurchaseOrderLine.Quantity,
                l.ReceivedQuantity,
                l.ReturnedQuantity,
                l.SortOrder, l.LineNotes,
                l.LotNumber, l.Shade, l.ManufactureDate, l.ExpiryDate))
            .ToList();

        var dto = new GoodsReceiptDto(
            grn.Id, grn.Code,
            grn.PurchaseOrderId, grn.PurchaseOrder.Code,
            grn.PurchaseOrder.SupplierId, grn.PurchaseOrder.Supplier.Name,
            grn.ReceiveDate,
            grn.ReceivingWarehouseId, grn.ReceivingWarehouse.Name,
            grn.Status.ToString(),
            grn.SupplierDeliveryRef,
            grn.PostedAt, grn.PostedBy, grn.Notes,
            lines,
            grn.LetterOfCreditId,
            grn.LetterOfCredit != null ? grn.LetterOfCredit.Code : null,
            grn.LetterOfCredit != null ? grn.LetterOfCredit.LcNumber : null,
            grn.LetterOfCredit != null ? grn.LetterOfCredit.Status.ToString() : null);

        return ApiResponse<GoodsReceiptDto>.Ok(dto);
    }
}
