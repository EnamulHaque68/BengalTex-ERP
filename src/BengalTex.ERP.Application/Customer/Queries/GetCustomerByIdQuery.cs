using BengalTex.ERP.Application.Customer.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Customer.Queries;

public sealed record GetCustomerByIdQuery(int Id) : IRequest<ApiResponse<CustomerDto>>;

internal sealed class GetCustomerByIdQueryHandler
    : IRequestHandler<GetCustomerByIdQuery, ApiResponse<CustomerDto>>
{
    private readonly IRepository<Domain.Entities.Customer> _repo;
    private readonly IMapper _mapper;

    public GetCustomerByIdQueryHandler(IRepository<Domain.Entities.Customer> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CustomerDto>> Handle(
        GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return entity is null
            ? ApiResponse<CustomerDto>.Fail("Customer not found.")
            : ApiResponse<CustomerDto>.Ok(_mapper.Map<CustomerDto>(entity));
    }
}
