using BengalTex.ERP.Application.RawMaterial.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.RawMaterial.Queries;

public sealed record GetRawMaterialByIdQuery(int Id) : IRequest<ApiResponse<RawMaterialDto>>;

internal sealed class GetRawMaterialByIdQueryHandler
    : IRequestHandler<GetRawMaterialByIdQuery, ApiResponse<RawMaterialDto>>
{
    private readonly IRepository<Domain.Entities.RawMaterial> _repo;

    public GetRawMaterialByIdQueryHandler(IRepository<Domain.Entities.RawMaterial> repo) => _repo = repo;

    public async Task<ApiResponse<RawMaterialDto>> Handle(
        GetRawMaterialByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _repo.Query()
            .Where(r => r.Id == request.Id)
            .Select(r => new RawMaterialDto(
                r.Id, r.Code, r.Name, r.Specification,
                r.Category.ToString(),
                r.UnitOfMeasureId, r.UnitOfMeasure.Code,
                r.MinimumStockLevel, r.OpeningStock, r.StandardCost,
                r.PreferredSupplierId,
                r.PreferredSupplier != null ? r.PreferredSupplier.Name : null,
                r.Notes, r.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ApiResponse<RawMaterialDto>.Fail("Raw material not found.")
            : ApiResponse<RawMaterialDto>.Ok(dto);
    }
}
