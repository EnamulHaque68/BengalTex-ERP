using BengalTex.ERP.Application.Factory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Factory.Queries;


public sealed record GetFactoryByIdQuery(int Id) : IRequest<ApiResponse<FactoryDto>>;

internal sealed class GetFactoryByIdQueryHandler : IRequestHandler<GetFactoryByIdQuery, ApiResponse<FactoryDto>>
{
    private readonly IRepository<Domain.Entities.Factory> _repo;
    private readonly IMapper _mapper;

    public GetFactoryByIdQueryHandler(IRepository<Domain.Entities.Factory> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ApiResponse<FactoryDto>> Handle(GetFactoryByIdQuery request, CancellationToken cancellationToken)
    {
        var factory = await _repo.GetByIdAsync(request.Id, cancellationToken);

        if (factory is null)
            return ApiResponse<FactoryDto>.Fail("Factory not found.");

        return ApiResponse<FactoryDto>.Ok(_mapper.Map<FactoryDto>(factory));
    }
}
