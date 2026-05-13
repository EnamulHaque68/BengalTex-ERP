using BengalTex.ERP.Application.Currency.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Currency.Queries;

public sealed record GetCurrenciesQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<List<CurrencyDto>>>;

internal sealed class GetCurrenciesQueryHandler
    : IRequestHandler<GetCurrenciesQuery, ApiResponse<List<CurrencyDto>>>
{
    private readonly IRepository<Domain.Entities.Currency> _repo;

    public GetCurrenciesQueryHandler(IRepository<Domain.Entities.Currency> repo) => _repo = repo;

    public async Task<ApiResponse<List<CurrencyDto>>> Handle(
        GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();
        if (!request.IncludeInactive)
            query = query.Where(c => c.IsActive);

        var list = await query
            .OrderByDescending(c => c.IsBaseCurrency)
            .ThenBy(c => c.Code)
            .ProjectToType<CurrencyDto>()
            .ToListAsync(cancellationToken);

        return ApiResponse<List<CurrencyDto>>.Ok(list);
    }
}
