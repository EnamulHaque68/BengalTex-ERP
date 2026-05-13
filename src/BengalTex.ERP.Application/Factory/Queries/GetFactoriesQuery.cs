using BengalTex.ERP.Application.Factory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Factory.Queries;

public sealed record GetFactoriesQuery(bool IncludeInactive = false) : IRequest<ApiResponse<List<FactoryListItemDto>>>;

internal sealed class GetFactoriesQueryHandler : IRequestHandler<GetFactoriesQuery, ApiResponse<List<FactoryListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Factory> _repo;

    public GetFactoriesQueryHandler(IRepository<Domain.Entities.Factory> repo) => _repo = repo;

    public async Task<ApiResponse<List<FactoryListItemDto>>> Handle(GetFactoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!request.IncludeInactive)
            query = query.Where(f => f.IsActive);

        // ProjectToType runs the mapping in SQL — only selected columns hit the DB.
        // Uses TypeAdapterConfig.GlobalSettings (registered in Application DI).
        var list = await query
            .OrderBy(f => f.Name)
            .ProjectToType<FactoryListItemDto>()
            .ToListAsync(cancellationToken);

        return ApiResponse<List<FactoryListItemDto>>.Ok(list);
    }
}
