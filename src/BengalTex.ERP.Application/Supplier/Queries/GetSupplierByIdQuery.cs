using BengalTex.ERP.Application.Supplier.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Supplier.Queries;

public sealed record GetSupplierByIdQuery(int Id) : IRequest<ApiResponse<SupplierDto>>;

internal sealed class GetSupplierByIdQueryHandler
    : IRequestHandler<GetSupplierByIdQuery, ApiResponse<SupplierDto>>
{
    private readonly IRepository<Domain.Entities.Supplier> _repo;
    private readonly IMapper _mapper;

    public GetSupplierByIdQueryHandler(IRepository<Domain.Entities.Supplier> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ApiResponse<SupplierDto>> Handle(
        GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return entity is null
            ? ApiResponse<SupplierDto>.Fail("Supplier not found.")
            : ApiResponse<SupplierDto>.Ok(_mapper.Map<SupplierDto>(entity));
    }
}
