using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Stock ledger / item ledger for a single item — running balance from StockMovement rows.
/// <paramref name="ItemType"/> ("RawMaterial" | "Product") + <paramref name="ItemId"/> are
/// required; <paramref name="WarehouseId"/> scopes to one warehouse (null = all); the date
/// range bounds the listed rows while the opening balance carries everything before it.
/// </summary>
public sealed record GetStockLedgerReportQuery(
    string ItemType,
    int ItemId,
    int? WarehouseId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<StockLedgerReportDto>>;

internal sealed class GetStockLedgerReportQueryHandler
    : IRequestHandler<GetStockLedgerReportQuery, ApiResponse<StockLedgerReportDto>>
{
    private readonly IRepository<Domain.Entities.StockMovement, long> _movementRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;

    public GetStockLedgerReportQueryHandler(
        IRepository<Domain.Entities.StockMovement, long> movementRepo,
        IRepository<Domain.Entities.RawMaterial> rmRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo)
    {
        _movementRepo = movementRepo;
        _rmRepo = rmRepo;
        _productRepo = productRepo;
        _warehouseRepo = warehouseRepo;
    }

    public async Task<ApiResponse<StockLedgerReportDto>> Handle(
        GetStockLedgerReportQuery req, CancellationToken ct)
    {
        var isRm = string.Equals(req.ItemType, "RawMaterial", StringComparison.OrdinalIgnoreCase);
        var isProduct = string.Equals(req.ItemType, "Product", StringComparison.OrdinalIgnoreCase);
        if (!isRm && !isProduct)
            return ApiResponse<StockLedgerReportDto>.Fail("ItemType must be 'RawMaterial' or 'Product'.");

        // Resolve the item's display fields + current weighted-average cost.
        string itemCode, itemName, uomCode;
        decimal unitCost;
        if (isRm)
        {
            var rm = await _rmRepo.Query().Include(r => r.UnitOfMeasure)
                .FirstOrDefaultAsync(r => r.Id == req.ItemId, ct);
            if (rm is null) return ApiResponse<StockLedgerReportDto>.Fail("Raw material not found.");
            itemCode = rm.Code; itemName = rm.Name; uomCode = rm.UnitOfMeasure.Code; unitCost = rm.WeightedAverageCost;
        }
        else
        {
            var p = await _productRepo.Query().Include(x => x.UnitOfMeasure)
                .FirstOrDefaultAsync(x => x.Id == req.ItemId, ct);
            if (p is null) return ApiResponse<StockLedgerReportDto>.Fail("Product not found.");
            itemCode = p.Code; itemName = p.Name; uomCode = p.UnitOfMeasure.Code; unitCost = p.WeightedAverageCost;
        }

        string? warehouseName = null;
        if (req.WarehouseId.HasValue)
        {
            var wh = await _warehouseRepo.GetByIdAsync(req.WarehouseId.Value, ct);
            if (wh is null) return ApiResponse<StockLedgerReportDto>.Fail("Warehouse not found.");
            warehouseName = wh.Name;
        }

        // Base filter: this item (+ optional warehouse).
        var baseQuery = _movementRepo.Query().Where(m =>
            isRm ? m.RawMaterialId == req.ItemId : m.ProductId == req.ItemId);
        if (req.WarehouseId.HasValue)
            baseQuery = baseQuery.Where(m => m.WarehouseId == req.WarehouseId.Value);

        // Opening balance = everything strictly before FromDate (0 when no FromDate).
        var openingBalance = 0m;
        if (req.FromDate.HasValue)
        {
            openingBalance = await baseQuery
                .Where(m => m.MovementDate < req.FromDate.Value)
                .SumAsync(m => (decimal?)m.SignedQuantity, ct) ?? 0m;
        }

        // Rows within the range, ordered chronologically then by insertion id.
        var rangeQuery = baseQuery;
        if (req.FromDate.HasValue) rangeQuery = rangeQuery.Where(m => m.MovementDate >= req.FromDate.Value);
        if (req.ToDate.HasValue) rangeQuery = rangeQuery.Where(m => m.MovementDate <= req.ToDate.Value);

        var movements = await rangeQuery
            .OrderBy(m => m.MovementDate).ThenBy(m => m.Id)
            .Select(m => new
            {
                m.MovementDate,
                m.Code,
                m.MovementType,
                m.ReferenceType,
                m.ReferenceCode,
                WarehouseCode = m.Warehouse.Code,
                LotNumber = m.Lot != null ? m.Lot.LotNumber : null,
                m.SignedQuantity,
                m.Notes
            })
            .ToListAsync(ct);

        var rows = new List<StockLedgerRowDto>(movements.Count);
        var running = openingBalance;
        decimal totalIn = 0m, totalOut = 0m;
        foreach (var m in movements)
        {
            running += m.SignedQuantity;
            var inQty = m.SignedQuantity > 0m ? m.SignedQuantity : 0m;
            var outQty = m.SignedQuantity < 0m ? -m.SignedQuantity : 0m;
            totalIn += inQty;
            totalOut += outQty;
            rows.Add(new StockLedgerRowDto(
                m.MovementDate, m.Code, m.MovementType.ToString(),
                m.ReferenceType, m.ReferenceCode, m.WarehouseCode, m.LotNumber,
                inQty, outQty, running, m.Notes));
        }

        var closing = running;
        var dto = new StockLedgerReportDto(
            GeneratedAt: DateTimeOffset.UtcNow,
            ItemType: isRm ? "RawMaterial" : "Product",
            ItemId: req.ItemId,
            ItemCode: itemCode,
            ItemName: itemName,
            UnitOfMeasureCode: uomCode,
            WarehouseId: req.WarehouseId,
            WarehouseName: warehouseName,
            FromDate: req.FromDate,
            ToDate: req.ToDate,
            OpeningBalance: openingBalance,
            TotalIn: totalIn,
            TotalOut: totalOut,
            ClosingBalance: closing,
            UnitCost: unitCost,
            ClosingValue: closing * unitCost,
            Rows: rows);

        return ApiResponse<StockLedgerReportDto>.Ok(dto);
    }
}
