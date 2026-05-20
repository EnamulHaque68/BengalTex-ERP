using BengalTex.ERP.Application.SupplierReturnNote.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierReturnNote.Queries;

public sealed record GetSupplierReturnNoteByIdQuery(long Id) : IRequest<ApiResponse<SupplierReturnNoteDto>>;

internal sealed class GetSupplierReturnNoteByIdQueryHandler
    : IRequestHandler<GetSupplierReturnNoteByIdQuery, ApiResponse<SupplierReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.SupplierReturnNote, long> _repo;

    public GetSupplierReturnNoteByIdQueryHandler(IRepository<Domain.Entities.SupplierReturnNote, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<SupplierReturnNoteDto>> Handle(
        GetSupplierReturnNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var srn = await _repo.Query()
            .AsNoTracking()
            .Include(s => s.GoodsReceiptNote).ThenInclude(g => g.PurchaseOrder).ThenInclude(p => p.Supplier)
            .Include(s => s.ReturnFromWarehouse)
            .Include(s => s.Lines).ThenInclude(l => l.GoodsReceiptLine)
            .Include(s => s.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm.UnitOfMeasure)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (srn is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Supplier return note not found.");

        var lines = srn.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l =>
            {
                var includesSelf = srn.Status == Domain.Entities.SupplierReturnNoteStatus.Posted;
                var previouslyReturned = includesSelf
                    ? l.GoodsReceiptLine.ReturnedQuantity - l.ReturnedQuantity
                    : l.GoodsReceiptLine.ReturnedQuantity;

                return new SupplierReturnNoteLineDto(
                    l.Id,
                    l.GoodsReceiptLineId,
                    l.RawMaterialId,
                    l.RawMaterial.Code,
                    l.RawMaterial.Name,
                    l.RawMaterial.UnitOfMeasure.Code,
                    l.GoodsReceiptLine.ReceivedQuantity,
                    previouslyReturned,
                    l.ReturnedQuantity,
                    l.GoodsReceiptLine.ReceivedQuantity - previouslyReturned,
                    l.SortOrder,
                    l.LineNotes);
            })
            .ToList();

        var dto = new SupplierReturnNoteDto(
            srn.Id, srn.Code,
            srn.GoodsReceiptNoteId, srn.GoodsReceiptNote.Code,
            srn.GoodsReceiptNote.PurchaseOrderId, srn.GoodsReceiptNote.PurchaseOrder.Code,
            srn.GoodsReceiptNote.PurchaseOrder.SupplierId,
            srn.GoodsReceiptNote.PurchaseOrder.Supplier.Name,
            srn.ReturnDate,
            srn.ReturnFromWarehouseId, srn.ReturnFromWarehouse.Code, srn.ReturnFromWarehouse.Name,
            srn.Status.ToString(),
            srn.VehicleNumber, srn.Reason,
            srn.PostedAt, srn.PostedBy, srn.Notes,
            lines);

        return ApiResponse<SupplierReturnNoteDto>.Ok(dto);
    }
}
