using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Queries;

public sealed record GetProductionOrderByIdQuery(long Id) : IRequest<ApiResponse<ProductionOrderDto>>;

internal sealed class GetProductionOrderByIdQueryHandler
    : IRequestHandler<GetProductionOrderByIdQuery, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IRepository<Domain.Entities.StockOnHand> _onHandRepo;
    private readonly Services.IStockReservationService _reservations;

    public GetProductionOrderByIdQueryHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.StockOnHand> onHandRepo,
        Services.IStockReservationService reservations)
    {
        _repo = repo;
        _onHandRepo = onHandRepo;
        _reservations = reservations;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        GetProductionOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .AsNoTracking()
            .Include(p => p.Product).ThenInclude(prod => prod.UnitOfMeasure)
            .Include(p => p.IssueWarehouse)
            .Include(p => p.ReceiveWarehouse)
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm!.UnitOfMeasure)
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.ComponentProduct).ThenInclude(cp => cp!.UnitOfMeasure)
            .Include(p => p.Stages).ThenInclude(s => s.OperatorEmployee)
            .Include(p => p.Stages).ThenInclude(s => s.WorkCenter)
            .Include(p => p.Stages).ThenInclude(s => s.Shift)
            .Include(p => p.SalesOrder).ThenInclude(so => so!.Customer)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");

        // Compute scaling factor and pull current on-hand for each component in the issue warehouse
        var scale = po.Bom.OutputQuantity > 0 ? po.Quantity / po.Bom.OutputQuantity : 0m;
        var rawMaterialIds = po.Bom.Lines.Where(l => l.RawMaterialId != null).Select(l => l.RawMaterialId!.Value).Distinct().ToList();
        var componentProductIds = po.Bom.Lines.Where(l => l.ComponentProductId != null).Select(l => l.ComponentProductId!.Value).Distinct().ToList();

        var rmOnHand = await _onHandRepo.Query()
            .Where(s => s.WarehouseId == po.IssueWarehouseId
                && s.RawMaterialId != null
                && rawMaterialIds.Contains(s.RawMaterialId!.Value))
            .GroupBy(s => s.RawMaterialId!.Value)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty, cancellationToken);

        var productOnHand = await _onHandRepo.Query()
            .Where(s => s.WarehouseId == po.IssueWarehouseId
                && s.ProductId != null
                && componentProductIds.Contains(s.ProductId!.Value))
            .GroupBy(s => s.ProductId!.Value)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty, cancellationToken);

        var plannedLines = po.Bom.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l =>
            {
                var scaledQty = l.Quantity * (1 + l.WastagePercent / 100m) * scale;
                var isRm = l.RawMaterialId is not null;
                var itemId = isRm ? l.RawMaterialId!.Value : l.ComponentProductId!.Value;
                var onHand = isRm
                    ? (rmOnHand.TryGetValue(itemId, out var q) ? q : 0m)
                    : (productOnHand.TryGetValue(itemId, out var pq) ? pq : 0m);
                var code = isRm ? l.RawMaterial!.Code : l.ComponentProduct!.Code;
                var name = isRm ? l.RawMaterial!.Name : l.ComponentProduct!.Name;
                var uom = isRm ? l.RawMaterial!.UnitOfMeasure.Code : l.ComponentProduct!.UnitOfMeasure.Code;
                return new ProductionPlannedLineDto(
                    isRm ? "RawMaterial" : "Product", itemId, code, name, uom,
                    l.Quantity, l.WastagePercent,
                    scaledQty, onHand,
                    onHand >= scaledQty);
            })
            .ToList();

        var stages = po.Stages
            .OrderBy(s => s.Sequence)
            .Select(s => new ProductionStageDto(
                s.Id, s.Sequence, s.StageName, s.Status.ToString(),
                s.PlannedQuantity, s.CompletedQuantity, s.RejectedQuantity,
                s.ProductionLine, s.OperatorEmployeeId,
                s.OperatorEmployee != null ? s.OperatorEmployee.FullName : null,
                s.StartedAt, s.CompletedAt, s.Notes,
                s.WorkCenterId,
                s.WorkCenter != null ? s.WorkCenter.Name : null,
                s.ShiftId,
                s.Shift != null ? s.Shift.Name : null))
            .ToList();

        var qcHeldQty = po.RequiresQc
            ? await _reservations.GetReservedForReferenceAsync("QcHold", po.Id, cancellationToken)
            : 0m;

        // Material cost: use the value captured at Complete; if it wasn't captured (order completed
        // before cost-capture existed, or still 0), fall back to the live BOM × weighted-average-cost.
        var computedMaterialCost = Math.Round(po.Bom.Lines.Sum(l =>
        {
            var scaledQty = l.Quantity * (1 + l.WastagePercent / 100m) * scale;
            var wac = l.RawMaterialId != null
                ? (l.RawMaterial?.WeightedAverageCost ?? 0m)
                : (l.ComponentProduct?.WeightedAverageCost ?? 0m);
            return scaledQty * wac;
        }), 2);
        var materialCost = po.MaterialCost > 0m ? po.MaterialCost : computedMaterialCost;

        var totalCost = materialCost + po.LabourCost + po.MachineCost + po.OverheadCost
                      + po.SubcontractCost + po.WastageCost + po.RejectCost;
        var costPerUnit = po.Quantity > 0m ? Math.Round(totalCost / po.Quantity, 4) : 0m;

        var dto = new ProductionOrderDto(
            po.Id, po.Code,
            po.ProductId, po.Product.Code, po.Product.Name, po.Product.UnitOfMeasure.Code,
            po.BomId, po.Bom.Code, po.Bom.Version, po.Bom.OutputQuantity,
            po.Quantity,
            po.IssueWarehouseId, po.IssueWarehouse.Code, po.IssueWarehouse.Name,
            po.ReceiveWarehouseId, po.ReceiveWarehouse.Code, po.ReceiveWarehouse.Name,
            po.PlannedStartDate, po.PlannedEndDate,
            po.ActualStartDate, po.ActualEndDate,
            po.Status.ToString(),
            po.CompletedAt, po.CompletedBy, po.Notes,
            plannedLines, stages,
            po.SalesOrderId,
            po.SalesOrder != null ? po.SalesOrder.Code : null,
            po.SalesOrderLineId,
            po.SalesOrder != null ? po.SalesOrder.Customer.Name : null,
            po.RequiresQc,
            po.QcReleasedAt,
            qcHeldQty > 0m,
            qcHeldQty,
            materialCost, po.LabourCost, po.MachineCost, po.OverheadCost,
            po.SubcontractCost, po.WastageCost, po.RejectCost,
            totalCost, costPerUnit);

        return ApiResponse<ProductionOrderDto>.Ok(dto);
    }
}
