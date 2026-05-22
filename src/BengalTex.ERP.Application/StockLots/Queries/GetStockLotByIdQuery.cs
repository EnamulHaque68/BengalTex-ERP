using BengalTex.ERP.Application.StockLots.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.StockLots.Queries;

/// <summary>A single lot plus the stock movements tagged to it (traceability trail).</summary>
public sealed record GetStockLotByIdQuery(long Id) : IRequest<ApiResponse<StockLotDetailDto>>;

internal sealed class GetStockLotByIdQueryHandler
    : IRequestHandler<GetStockLotByIdQuery, ApiResponse<StockLotDetailDto>>
{
    private readonly IRepository<StockLot, long> _lotRepo;
    private readonly IRepository<StockMovement, long> _movementRepo;

    public GetStockLotByIdQueryHandler(
        IRepository<StockLot, long> lotRepo,
        IRepository<StockMovement, long> movementRepo)
    {
        _lotRepo = lotRepo;
        _movementRepo = movementRepo;
    }

    public async Task<ApiResponse<StockLotDetailDto>> Handle(
        GetStockLotByIdQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var lot = await _lotRepo.Query()
            .Where(l => l.Id == request.Id)
            .Select(l => new StockLotDto(
                l.Id, l.Code, l.LotNumber,
                l.RawMaterialId != null ? "RawMaterial" : "Product",
                l.RawMaterialId != null ? l.RawMaterialId!.Value : l.ProductId!.Value,
                l.RawMaterialId != null ? l.RawMaterial!.Code : l.Product!.Code,
                l.RawMaterialId != null ? l.RawMaterial!.Name : l.Product!.Name,
                l.RawMaterialId != null ? l.RawMaterial!.UnitOfMeasure.Code : l.Product!.UnitOfMeasure.Code,
                l.WarehouseId, l.Warehouse.Name,
                l.SupplierId, l.Supplier != null ? l.Supplier.Name : null,
                l.Shade,
                l.ReceivedDate, l.ManufactureDate, l.ExpiryDate,
                l.InitialQuantity, l.CurrentQuantity,
                l.Status.ToString(),
                l.ExpiryDate != null && l.ExpiryDate < today,
                l.SourceType, l.SourceId, l.SourceCode, l.Notes))
            .FirstOrDefaultAsync(cancellationToken);

        if (lot is null) return ApiResponse<StockLotDetailDto>.Fail("Stock lot not found.");

        var movements = await _movementRepo.Query()
            .Where(m => m.LotId == request.Id)
            .OrderBy(m => m.MovementDate).ThenBy(m => m.Id)
            .Select(m => new StockLotMovementDto(
                m.Id, m.Code, m.MovementType.ToString(), m.SignedQuantity,
                m.MovementDate, m.ReferenceType, m.ReferenceCode, m.Notes))
            .ToListAsync(cancellationToken);

        return ApiResponse<StockLotDetailDto>.Ok(new StockLotDetailDto(lot, movements));
    }
}
