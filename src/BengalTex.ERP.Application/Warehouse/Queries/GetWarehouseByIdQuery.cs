using BengalTex.ERP.Application.Warehouse.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Warehouse.Queries;

public sealed record GetWarehouseByIdQuery(int Id) : IRequest<ApiResponse<WarehouseDto>>;

internal sealed class GetWarehouseByIdQueryHandler
    : IRequestHandler<GetWarehouseByIdQuery, ApiResponse<WarehouseDto>>
{
    private readonly IRepository<Domain.Entities.Warehouse> _repo;

    public GetWarehouseByIdQueryHandler(IRepository<Domain.Entities.Warehouse> repo) => _repo = repo;

    public async Task<ApiResponse<WarehouseDto>> Handle(
        GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _repo.Query()
            .Where(w => w.Id == request.Id)
            .Select(w => new WarehouseDto(
                w.Id, w.Code, w.Name,
                w.WarehouseType.ToString(),
                w.Address,
                w.FactoryId,
                w.Factory != null ? w.Factory.Name : null,
                w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ApiResponse<WarehouseDto>.Fail("Warehouse not found.")
            : ApiResponse<WarehouseDto>.Ok(dto);
    }
}
