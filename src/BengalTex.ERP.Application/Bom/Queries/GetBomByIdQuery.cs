using BengalTex.ERP.Application.Bom.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Bom.Queries;

public sealed record GetBomByIdQuery(int Id) : IRequest<ApiResponse<BomDto>>;

internal sealed class GetBomByIdQueryHandler
    : IRequestHandler<GetBomByIdQuery, ApiResponse<BomDto>>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;

    public GetBomByIdQueryHandler(IRepository<Domain.Entities.Bom> repo) => _repo = repo;

    public async Task<ApiResponse<BomDto>> Handle(
        GetBomByIdQuery request, CancellationToken cancellationToken)
    {
        var bom = await _repo.Query()
            .AsNoTracking()
            .Include(b => b.Product).ThenInclude(p => p.UnitOfMeasure)
            .Include(b => b.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm!.UnitOfMeasure)
            .Include(b => b.Lines).ThenInclude(l => l.ComponentProduct).ThenInclude(p => p!.UnitOfMeasure)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (bom is null) return ApiResponse<BomDto>.Fail("BOM not found.");

        var lines = bom.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l =>
            {
                var effectiveQty = l.Quantity * (1 + l.WastagePercent / 100m);
                var isRm = l.RawMaterialId is not null;
                var code = isRm ? l.RawMaterial!.Code : l.ComponentProduct!.Code;
                var name = isRm ? l.RawMaterial!.Name : l.ComponentProduct!.Name;
                var uom = isRm ? l.RawMaterial!.UnitOfMeasure.Code : l.ComponentProduct!.UnitOfMeasure.Code;
                // RM costed at standard cost (existing convention); sub-assembly at its current WAC
                var unitCost = isRm ? l.RawMaterial!.StandardCost : l.ComponentProduct!.WeightedAverageCost;
                return new BomLineDto(
                    l.Id, isRm ? "RawMaterial" : "Product",
                    l.RawMaterialId, l.ComponentProductId, code, name, uom,
                    l.Quantity, l.WastagePercent, effectiveQty,
                    unitCost, effectiveQty * unitCost,
                    l.SortOrder, l.LineNotes);
            })
            .ToList();

        var totalCost = lines.Sum(l => l.LineCost);
        var dto = new BomDto(
            bom.Id, bom.Code, bom.ProductId, bom.Product.Code, bom.Product.Name,
            bom.Product.UnitOfMeasure.Code, bom.Version, bom.Name, bom.OutputQuantity,
            bom.Status.ToString(), bom.IsActive, bom.EffectiveDate,
            bom.ApprovedAt, bom.ApprovedBy, bom.Notes,
            totalCost, bom.OutputQuantity > 0 ? totalCost / bom.OutputQuantity : 0m,
            lines);

        return ApiResponse<BomDto>.Ok(dto);
    }
}
