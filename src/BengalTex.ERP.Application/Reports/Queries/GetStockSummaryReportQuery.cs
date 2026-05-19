using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Aggregated stock summary — one row per item (RawMaterial or Product) with total
/// quantity summed across all warehouses (or one specific warehouse if <see cref="WarehouseId"/>
/// is set). Distinct from <c>GetStockOnHandQuery</c> which returns per-(item,warehouse) rows.
/// Excludes zero-quantity items. No paging (report context — returns full result set).
/// </summary>
public sealed record GetStockSummaryReportQuery(
    string? ItemType = null,           // "RawMaterial" | "Product" | null = both
    int? WarehouseId = null
) : IRequest<ApiResponse<StockSummaryReportDto>>;

internal sealed class GetStockSummaryReportQueryHandler
    : IRequestHandler<GetStockSummaryReportQuery, ApiResponse<StockSummaryReportDto>>
{
    private readonly IRepository<Domain.Entities.StockOnHand> _stockRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;

    public GetStockSummaryReportQueryHandler(
        IRepository<Domain.Entities.StockOnHand> stockRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo)
    {
        _stockRepo = stockRepo;
        _warehouseRepo = warehouseRepo;
    }

    public async Task<ApiResponse<StockSummaryReportDto>> Handle(
        GetStockSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var includeRm = !string.Equals(request.ItemType, "Product", StringComparison.OrdinalIgnoreCase);
        var includeProduct = !string.Equals(request.ItemType, "RawMaterial", StringComparison.OrdinalIgnoreCase);

        string? warehouseName = null;
        if (request.WarehouseId.HasValue)
        {
            var wh = await _warehouseRepo.GetByIdAsync(request.WarehouseId.Value, cancellationToken);
            warehouseName = wh?.Name;
        }

        var rows = new List<StockSummaryRowDto>();
        decimal totalRmQty = 0m;
        decimal totalProductQty = 0m;

        if (includeRm)
        {
            var rmQuery = _stockRepo.Query().Where(s => s.RawMaterialId != null && s.Quantity != 0m);
            if (request.WarehouseId.HasValue)
                rmQuery = rmQuery.Where(s => s.WarehouseId == request.WarehouseId.Value);

            var rmRows = await rmQuery
                .GroupBy(s => new
                {
                    Id = s.RawMaterialId!.Value,
                    Code = s.RawMaterial!.Code,
                    Name = s.RawMaterial!.Name,
                    UomCode = s.RawMaterial!.UnitOfMeasure.Code
                })
                .Select(g => new StockSummaryRowDto(
                    "RawMaterial",
                    g.Key.Id,
                    null,
                    g.Key.Code,
                    g.Key.Name,
                    g.Key.UomCode,
                    g.Sum(x => x.Quantity),
                    g.Select(x => x.WarehouseId).Distinct().Count()))
                .ToListAsync(cancellationToken);

            rows.AddRange(rmRows);
            totalRmQty = rmRows.Sum(r => r.TotalQuantity);
        }

        if (includeProduct)
        {
            var productQuery = _stockRepo.Query().Where(s => s.ProductId != null && s.Quantity != 0m);
            if (request.WarehouseId.HasValue)
                productQuery = productQuery.Where(s => s.WarehouseId == request.WarehouseId.Value);

            var productRows = await productQuery
                .GroupBy(s => new
                {
                    Id = s.ProductId!.Value,
                    Code = s.Product!.Code,
                    Name = s.Product!.Name,
                    UomCode = s.Product!.UnitOfMeasure.Code
                })
                .Select(g => new StockSummaryRowDto(
                    "Product",
                    null,
                    g.Key.Id,
                    g.Key.Code,
                    g.Key.Name,
                    g.Key.UomCode,
                    g.Sum(x => x.Quantity),
                    g.Select(x => x.WarehouseId).Distinct().Count()))
                .ToListAsync(cancellationToken);

            rows.AddRange(productRows);
            totalProductQty = productRows.Sum(r => r.TotalQuantity);
        }

        var ordered = rows
            .OrderBy(r => r.ItemType)
            .ThenBy(r => r.Name)
            .ToList();

        var dto = new StockSummaryReportDto(
            GeneratedAt: DateTimeOffset.UtcNow,
            WarehouseId: request.WarehouseId,
            WarehouseName: warehouseName,
            ItemType: request.ItemType,
            RowCount: ordered.Count,
            TotalRawMaterialQuantity: totalRmQty,
            TotalProductQuantity: totalProductQty,
            Rows: ordered);

        return ApiResponse<StockSummaryReportDto>.Ok(dto);
    }
}
