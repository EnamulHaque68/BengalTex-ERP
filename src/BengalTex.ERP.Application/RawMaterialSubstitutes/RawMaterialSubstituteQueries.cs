using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.RawMaterialSubstitutes;

/// <summary>All approved substitutes for one raw material, with each substitute's current on-hand.</summary>
public sealed record GetRawMaterialSubstitutesQuery(int RawMaterialId)
    : IRequest<ApiResponse<IReadOnlyList<RawMaterialSubstituteDto>>>;

internal sealed class GetRawMaterialSubstitutesQueryHandler
    : IRequestHandler<GetRawMaterialSubstitutesQuery, ApiResponse<IReadOnlyList<RawMaterialSubstituteDto>>>
{
    private readonly IRepository<RawMaterialSubstitute> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IStockService _stock;

    public GetRawMaterialSubstitutesQueryHandler(
        IRepository<RawMaterialSubstitute> repo, IRepository<Domain.Entities.RawMaterial> rmRepo, IStockService stock)
    { _repo = repo; _rmRepo = rmRepo; _stock = stock; }

    public async Task<ApiResponse<IReadOnlyList<RawMaterialSubstituteDto>>> Handle(
        GetRawMaterialSubstitutesQuery req, CancellationToken ct)
    {
        if (await _rmRepo.GetByIdAsync(req.RawMaterialId, ct) is null)
            return ApiResponse<IReadOnlyList<RawMaterialSubstituteDto>>.Fail("Raw material not found.");

        var subs = await _repo.Query().AsNoTracking()
            .Where(s => s.RawMaterialId == req.RawMaterialId)
            .Include(s => s.SubstituteRawMaterial).ThenInclude(rm => rm.UnitOfMeasure)
            .OrderBy(s => s.SubstituteRawMaterial.Code)
            .ToListAsync(ct);

        var dtos = new List<RawMaterialSubstituteDto>();
        foreach (var s in subs)
        {
            var onHand = await _stock.GetRawMaterialTotalOnHandAsync(s.SubstituteRawMaterialId, ct);
            dtos.Add(new RawMaterialSubstituteDto(
                s.Id, s.RawMaterialId, s.SubstituteRawMaterialId,
                s.SubstituteRawMaterial.Code, s.SubstituteRawMaterial.Name, s.SubstituteRawMaterial.UnitOfMeasure.Code,
                s.ConversionFactor, onHand, s.Notes, s.IsActive));
        }
        return ApiResponse<IReadOnlyList<RawMaterialSubstituteDto>>.Ok(dtos);
    }
}
