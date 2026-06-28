using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Queries;

// ─── Traceability DTOs (Phase 7 — end-to-end manufacturing genealogy) ──────
public record TraceConsumedItemDto(
    string ItemType, string ItemCode, string ItemName, string UnitOfMeasureCode, decimal Quantity);

public record TraceJobCardDto(
    string Code, string Status, string? BatchNumber,
    string? OperatorName, string? MachineName,
    decimal Quantity, decimal CompletedQuantity, decimal RejectedQuantity);

public record TraceLotDto(
    string Code, string LotNumber, string? Shade, decimal CurrentQuantity);

public record ProductionTraceabilityDto(
    long ProductionOrderId,
    string Code,
    string Status,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    int BomVersion,
    string BomCode,
    // upstream demand chain
    string? SalesOrderCode,
    string? CustomerName,
    string? QuotationCode,
    // downstream / shop-floor
    IReadOnlyList<TraceConsumedItemDto> ConsumedItems,
    IReadOnlyList<TraceJobCardDto> JobCards,
    IReadOnlyList<TraceLotDto> Lots);

/// <summary>
/// End-to-end traceability for one production order (Phase 7): Finished Goods → Production Order →
/// Job Cards → Sales Order → Quotation → Customer → BOM → consumed Raw Materials + produced Lots.
/// Read-only link-chain over data linked in earlier phases (no new schema).
/// </summary>
public sealed record GetProductionTraceabilityQuery(long ProductionOrderId)
    : IRequest<ApiResponse<ProductionTraceabilityDto>>;

internal sealed class GetProductionTraceabilityQueryHandler
    : IRequestHandler<GetProductionTraceabilityQuery, ApiResponse<ProductionTraceabilityDto>>
{
    private readonly IRepository<ProductionOrder, long> _repo;
    private readonly IRepository<StockMovement, long> _moveRepo;
    private readonly IRepository<JobCard, long> _jcRepo;
    private readonly IRepository<StockLot, long> _lotRepo;
    private readonly IRepository<Domain.Entities.Quotation, long> _quotRepo;

    public GetProductionTraceabilityQueryHandler(
        IRepository<ProductionOrder, long> repo,
        IRepository<StockMovement, long> moveRepo,
        IRepository<JobCard, long> jcRepo,
        IRepository<StockLot, long> lotRepo,
        IRepository<Domain.Entities.Quotation, long> quotRepo)
    {
        _repo = repo;
        _moveRepo = moveRepo;
        _jcRepo = jcRepo;
        _lotRepo = lotRepo;
        _quotRepo = quotRepo;
    }

    public async Task<ApiResponse<ProductionTraceabilityDto>> Handle(
        GetProductionTraceabilityQuery request, CancellationToken ct)
    {
        var po = await _repo.Query()
            .AsNoTracking()
            .Include(p => p.Product)
            .Include(p => p.Bom)
            .Include(p => p.SalesOrder).ThenInclude(so => so!.Customer)
            .FirstOrDefaultAsync(p => p.Id == request.ProductionOrderId, ct);
        if (po is null) return ApiResponse<ProductionTraceabilityDto>.Fail("Production order not found.");

        // Quotation that drove the source sales order (direct convert), if any.
        string? quotationCode = null;
        if (po.SalesOrderId is long soId)
        {
            quotationCode = await _quotRepo.Query().AsNoTracking()
                .Where(q => q.ConvertedSalesOrderId == soId)
                .Select(q => q.Code)
                .FirstOrDefaultAsync(ct);
        }

        // Consumed raw materials / components — the ProductionIssue movements.
        var consumed = await _moveRepo.Query()
            .AsNoTracking()
            .Where(m => m.ReferenceType == "ProductionOrder" && m.ReferenceId == po.Id
                     && m.MovementType == StockMovementType.ProductionIssue)
            .Select(m => new
            {
                IsRm = m.RawMaterialId != null,
                Code = m.RawMaterialId != null ? m.RawMaterial!.Code : m.Product!.Code,
                Name = m.RawMaterialId != null ? m.RawMaterial!.Name : m.Product!.Name,
                Uom = m.RawMaterialId != null ? m.RawMaterial!.UnitOfMeasure.Code : m.Product!.UnitOfMeasure.Code,
                m.SignedQuantity
            })
            .ToListAsync(ct);

        var consumedItems = consumed
            .GroupBy(x => new { x.IsRm, x.Code, x.Name, x.Uom })
            .Select(g => new TraceConsumedItemDto(
                g.Key.IsRm ? "RawMaterial" : "Component",
                g.Key.Code, g.Key.Name, g.Key.Uom,
                Math.Abs(g.Sum(x => x.SignedQuantity))))
            .OrderBy(x => x.ItemName)
            .ToList();

        var jobCards = await _jcRepo.Query()
            .AsNoTracking()
            .Where(j => j.ProductionOrderId == po.Id)
            .OrderBy(j => j.Code)
            .Select(j => new TraceJobCardDto(
                j.Code, j.Status.ToString(), j.BatchNumber,
                j.OperatorEmployee != null ? j.OperatorEmployee.FullName : null,
                j.Machine != null ? j.Machine.Name : null,
                j.Quantity, j.CompletedQuantity, j.RejectedQuantity))
            .ToListAsync(ct);

        var lots = await _lotRepo.Query()
            .AsNoTracking()
            .Where(l => l.SourceCode == po.Code)
            .OrderBy(l => l.LotNumber)
            .Select(l => new TraceLotDto(l.Code, l.LotNumber, l.Shade, l.CurrentQuantity))
            .ToListAsync(ct);

        var dto = new ProductionTraceabilityDto(
            po.Id, po.Code, po.Status.ToString(),
            po.Product.Code, po.Product.Name, po.Quantity,
            po.ActualStartDate, po.ActualEndDate,
            po.Bom.Version, po.Bom.Code,
            po.SalesOrder?.Code,
            po.SalesOrder?.Customer.Name,
            quotationCode,
            consumedItems, jobCards, lots);

        return ApiResponse<ProductionTraceabilityDto>.Ok(dto);
    }
}
