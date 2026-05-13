using BengalTex.ERP.Application.UnitOfMeasure.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.UnitOfMeasure.Queries;

public sealed record GetUnitOfMeasureByIdQuery(int Id) : IRequest<ApiResponse<UnitOfMeasureDto>>;

internal sealed class GetUnitOfMeasureByIdQueryHandler
    : IRequestHandler<GetUnitOfMeasureByIdQuery, ApiResponse<UnitOfMeasureDto>>
{
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _repo;

    public GetUnitOfMeasureByIdQueryHandler(IRepository<Domain.Entities.UnitOfMeasure> repo) => _repo = repo;

    public async Task<ApiResponse<UnitOfMeasureDto>> Handle(
        GetUnitOfMeasureByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _repo.Query()
            .Where(u => u.Id == request.Id)
            .Select(u => new UnitOfMeasureDto(
                u.Id, u.Code, u.Name, u.Symbol,
                u.UnitType.ToString(),
                u.BaseUnitId,
                u.BaseUnit != null ? u.BaseUnit.Code : null,
                u.ConversionFactor,
                u.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ApiResponse<UnitOfMeasureDto>.Fail("Unit of measure not found.")
            : ApiResponse<UnitOfMeasureDto>.Ok(dto);
    }
}
