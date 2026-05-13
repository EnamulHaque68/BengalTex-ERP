using BengalTex.ERP.Application.Currency.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MapsterMapper;
using MediatR;

namespace BengalTex.ERP.Application.Currency.Queries;

public sealed record GetCurrencyByIdQuery(int Id) : IRequest<ApiResponse<CurrencyDto>>;

internal sealed class GetCurrencyByIdQueryHandler
    : IRequestHandler<GetCurrencyByIdQuery, ApiResponse<CurrencyDto>>
{
    private readonly IRepository<Domain.Entities.Currency> _repo;
    private readonly IMapper _mapper;

    public GetCurrencyByIdQueryHandler(IRepository<Domain.Entities.Currency> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CurrencyDto>> Handle(GetCurrencyByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return entity is null
            ? ApiResponse<CurrencyDto>.Fail("Currency not found.")
            : ApiResponse<CurrencyDto>.Ok(_mapper.Map<CurrencyDto>(entity));
    }
}
