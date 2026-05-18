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

    public GetProductionOrderByIdQueryHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.StockOnHand> onHandRepo)
    {
        _repo = repo;
        _onHandRepo = onHandRepo;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        GetProductionOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .AsNoTracking()
            .Include(p => p.Product).ThenInclude(prod => prod.UnitOfMeasure)
            .Include(p => p.IssueWarehouse)
            .Include(p => p.ReceiveWarehouse)
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");

        // Compute scaling factor and pull current on-hand for each RM in the issue warehouse
        var scale = po.Bom.OutputQuantity > 0 ? po.Quantity / po.Bom.OutputQuantity : 0m;
        var rawMaterialIds = po.Bom.Lines.Select(l => l.RawMaterialId).Distinct().ToList();

        var onHandLookup = await _onHandRepo.Query()
            .Where(s => s.WarehouseId == po.IssueWarehouseId
                && s.RawMaterialId != null
                && rawMaterialIds.Contains(s.RawMaterialId!.Value))
            .ToDictionaryAsync(s => s.RawMaterialId!.Value, s => s.Quantity, cancellationToken);

        var plannedLines = po.Bom.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l =>
            {
                var scaledQty = l.Quantity * (1 + l.WastagePercent / 100m) * scale;
                var onHand = onHandLookup.TryGetValue(l.RawMaterialId, out var q) ? q : 0m;
                return new ProductionPlannedLineDto(
                    l.RawMaterialId, l.RawMaterial.Code, l.RawMaterial.Name,
                    l.RawMaterial.UnitOfMeasure.Code,
                    l.Quantity, l.WastagePercent,
                    scaledQty, onHand,
                    onHand >= scaledQty);
            })
            .ToList();

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
            plannedLines);

        return ApiResponse<ProductionOrderDto>.Ok(dto);
    }
}
