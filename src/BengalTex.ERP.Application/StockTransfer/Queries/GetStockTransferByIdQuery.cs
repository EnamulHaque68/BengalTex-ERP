using BengalTex.ERP.Application.StockTransfer.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.StockTransfer.Queries;

public sealed record GetStockTransferByIdQuery(long Id) : IRequest<ApiResponse<StockTransferDto>>;

internal sealed class GetStockTransferByIdQueryHandler
    : IRequestHandler<GetStockTransferByIdQuery, ApiResponse<StockTransferDto>>
{
    private readonly IRepository<Domain.Entities.StockTransfer, long> _repo;

    public GetStockTransferByIdQueryHandler(IRepository<Domain.Entities.StockTransfer, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<StockTransferDto>> Handle(
        GetStockTransferByIdQuery request, CancellationToken cancellationToken)
    {
        var t = await _repo.Query()
            .AsNoTracking()
            .Include(s => s.SourceWarehouse)
            .Include(s => s.DestinationWarehouse)
            .Include(s => s.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm!.UnitOfMeasure)
            .Include(s => s.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.UnitOfMeasure)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (t is null) return ApiResponse<StockTransferDto>.Fail("Stock transfer not found.");

        var lines = t.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new StockTransferLineDto(
                l.Id,
                l.RawMaterialId.HasValue ? "RawMaterial" : "Product",
                l.RawMaterialId,
                l.ProductId,
                l.RawMaterialId.HasValue ? l.RawMaterial!.Code : l.Product!.Code,
                l.RawMaterialId.HasValue ? l.RawMaterial!.Name : l.Product!.Name,
                l.RawMaterialId.HasValue ? l.RawMaterial!.UnitOfMeasure.Code : l.Product!.UnitOfMeasure.Code,
                l.Quantity,
                l.SortOrder,
                l.LineNotes))
            .ToList();

        var dto = new StockTransferDto(
            t.Id, t.Code,
            t.SourceWarehouseId, t.SourceWarehouse.Code, t.SourceWarehouse.Name,
            t.DestinationWarehouseId, t.DestinationWarehouse.Code, t.DestinationWarehouse.Name,
            t.TransferDate,
            t.Status.ToString(),
            t.PostedAt, t.PostedBy,
            t.Notes,
            lines);

        return ApiResponse<StockTransferDto>.Ok(dto);
    }
}
