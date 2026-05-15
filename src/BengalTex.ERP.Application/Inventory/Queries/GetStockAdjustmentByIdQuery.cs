using BengalTex.ERP.Application.Inventory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Inventory.Queries;

public sealed record GetStockAdjustmentByIdQuery(long Id) : IRequest<ApiResponse<StockAdjustmentDto>>;

internal sealed class GetStockAdjustmentByIdQueryHandler
    : IRequestHandler<GetStockAdjustmentByIdQuery, ApiResponse<StockAdjustmentDto>>
{
    private readonly IRepository<Domain.Entities.StockAdjustment, long> _repo;

    public GetStockAdjustmentByIdQueryHandler(IRepository<Domain.Entities.StockAdjustment, long> repo) => _repo = repo;

    public async Task<ApiResponse<StockAdjustmentDto>> Handle(
        GetStockAdjustmentByIdQuery request, CancellationToken cancellationToken)
    {
        var adj = await _repo.Query()
            .AsNoTracking()
            .Include(a => a.Warehouse)
            .Include(a => a.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm.UnitOfMeasure)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (adj is null) return ApiResponse<StockAdjustmentDto>.Fail("Stock adjustment not found.");

        var lines = adj.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new StockAdjustmentLineDto(
                l.Id, l.RawMaterialId,
                l.RawMaterial.Code, l.RawMaterial.Name,
                l.RawMaterial.UnitOfMeasure.Code,
                l.SignedQuantity,
                l.SortOrder, l.LineNotes))
            .ToList();

        var dto = new StockAdjustmentDto(
            adj.Id, adj.Code,
            adj.AdjustmentDate,
            adj.WarehouseId, adj.Warehouse.Code, adj.Warehouse.Name,
            adj.Reason, adj.Status.ToString(),
            adj.PostedAt, adj.PostedBy, adj.Notes,
            lines);

        return ApiResponse<StockAdjustmentDto>.Ok(dto);
    }
}
