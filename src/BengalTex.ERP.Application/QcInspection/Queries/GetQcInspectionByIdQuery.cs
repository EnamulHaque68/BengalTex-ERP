using BengalTex.ERP.Application.QcInspection.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QcInspection.Queries;

public sealed record GetQcInspectionByIdQuery(long Id) : IRequest<ApiResponse<QcInspectionDto>>;

internal sealed class GetQcInspectionByIdQueryHandler
    : IRequestHandler<GetQcInspectionByIdQuery, ApiResponse<QcInspectionDto>>
{
    private readonly IRepository<Domain.Entities.QcInspection, long> _repo;
    private readonly IStockService _stock;

    public GetQcInspectionByIdQueryHandler(
        IRepository<Domain.Entities.QcInspection, long> repo,
        IStockService stock)
    {
        _repo = repo;
        _stock = stock;
    }

    public async Task<ApiResponse<QcInspectionDto>> Handle(
        GetQcInspectionByIdQuery request, CancellationToken cancellationToken)
    {
        var q = await _repo.Query()
            .AsNoTracking()
            .Include(x => x.GoodsReceiptNote).ThenInclude(g => g!.PurchaseOrder).ThenInclude(p => p.Supplier)
            .Include(x => x.ProductionOrder).ThenInclude(p => p!.Product)
            .Include(x => x.InspectedFromWarehouse)
            .Include(x => x.QuarantineWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm!.UnitOfMeasure)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (q is null) return ApiResponse<QcInspectionDto>.Fail("QC inspection not found.");

        var lines = new List<QcInspectionLineDto>();
        foreach (var l in q.Lines.OrderBy(l => l.SortOrder))
        {
            decimal availableAtSource;
            if (l.RawMaterialId.HasValue)
                availableAtSource = await _stock.GetRawMaterialOnHandAsync(
                    l.RawMaterialId.Value, q.InspectedFromWarehouseId, cancellationToken);
            else
                availableAtSource = await _stock.GetProductOnHandAsync(
                    l.ProductId!.Value, q.InspectedFromWarehouseId, cancellationToken);

            lines.Add(new QcInspectionLineDto(
                l.Id,
                l.RawMaterialId.HasValue ? "RawMaterial" : "Product",
                l.RawMaterialId,
                l.ProductId,
                l.RawMaterialId.HasValue ? l.RawMaterial!.Code : l.Product!.Code,
                l.RawMaterialId.HasValue ? l.RawMaterial!.Name : l.Product!.Name,
                l.RawMaterialId.HasValue ? l.RawMaterial!.UnitOfMeasure.Code : l.Product!.UnitOfMeasure.Code,
                l.InspectedQuantity,
                l.PassedQuantity,
                l.RejectedQuantity,
                availableAtSource,
                l.SortOrder,
                l.DefectNotes));
        }

        var sourceLabel = q.SourceType == Domain.Entities.QcSourceType.IncomingMaterial
            ? $"{q.GoodsReceiptNote!.Code} — {q.GoodsReceiptNote.PurchaseOrder.Supplier.Name}"
            : $"{q.ProductionOrder!.Code} — {q.ProductionOrder.Product.Name}";

        var dto = new QcInspectionDto(
            q.Id, q.Code,
            q.SourceType.ToString(),
            q.GoodsReceiptNoteId, q.GoodsReceiptNote?.Code,
            q.ProductionOrderId, q.ProductionOrder?.Code,
            sourceLabel,
            q.InspectionDate,
            q.InspectedFromWarehouseId, q.InspectedFromWarehouse.Name,
            q.QuarantineWarehouseId, q.QuarantineWarehouse.Name,
            q.Status.ToString(),
            q.OverallResult.ToString(),
            q.InspectedBy, q.Notes,
            lines);

        return ApiResponse<QcInspectionDto>.Ok(dto);
    }
}
