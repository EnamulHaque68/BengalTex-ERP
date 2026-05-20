using BengalTex.ERP.Application.QuarantineDisposition.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QuarantineDisposition.Queries;

public sealed record GetQuarantineDispositionByIdQuery(long Id) : IRequest<ApiResponse<QuarantineDispositionDto>>;

internal sealed class GetQuarantineDispositionByIdQueryHandler
    : IRequestHandler<GetQuarantineDispositionByIdQuery, ApiResponse<QuarantineDispositionDto>>
{
    private readonly IRepository<Domain.Entities.QuarantineDisposition, long> _repo;
    private readonly IStockService _stock;

    public GetQuarantineDispositionByIdQueryHandler(
        IRepository<Domain.Entities.QuarantineDisposition, long> repo,
        IStockService stock)
    {
        _repo = repo;
        _stock = stock;
    }

    public async Task<ApiResponse<QuarantineDispositionDto>> Handle(
        GetQuarantineDispositionByIdQuery request, CancellationToken cancellationToken)
    {
        var d = await _repo.Query()
            .AsNoTracking()
            .Include(x => x.QuarantineWarehouse)
            .Include(x => x.DestinationWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm!.UnitOfMeasure)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (d is null) return ApiResponse<QuarantineDispositionDto>.Fail("Quarantine disposition not found.");

        var lines = new List<QuarantineDispositionLineDto>();
        foreach (var l in d.Lines.OrderBy(l => l.SortOrder))
        {
            decimal available;
            if (l.RawMaterialId.HasValue)
                available = await _stock.GetRawMaterialOnHandAsync(
                    l.RawMaterialId.Value, d.QuarantineWarehouseId, cancellationToken);
            else
                available = await _stock.GetProductOnHandAsync(
                    l.ProductId!.Value, d.QuarantineWarehouseId, cancellationToken);

            lines.Add(new QuarantineDispositionLineDto(
                l.Id,
                l.RawMaterialId.HasValue ? "RawMaterial" : "Product",
                l.RawMaterialId,
                l.ProductId,
                l.RawMaterialId.HasValue ? l.RawMaterial!.Code : l.Product!.Code,
                l.RawMaterialId.HasValue ? l.RawMaterial!.Name : l.Product!.Name,
                l.RawMaterialId.HasValue ? l.RawMaterial!.UnitOfMeasure.Code : l.Product!.UnitOfMeasure.Code,
                l.Quantity,
                available,
                l.SortOrder,
                l.LineNotes));
        }

        var dto = new QuarantineDispositionDto(
            d.Id, d.Code,
            d.DispositionType.ToString(),
            d.DispositionDate,
            d.QuarantineWarehouseId, d.QuarantineWarehouse.Name,
            d.DestinationWarehouseId, d.DestinationWarehouse?.Name,
            d.Status.ToString(),
            d.Reason,
            d.PostedAt, d.PostedBy, d.Notes,
            lines);

        return ApiResponse<QuarantineDispositionDto>.Ok(dto);
    }
}
