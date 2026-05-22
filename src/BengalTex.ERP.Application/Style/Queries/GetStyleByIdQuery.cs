using BengalTex.ERP.Application.Style.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Style.Queries;

public sealed record GetStyleByIdQuery(int Id) : IRequest<ApiResponse<StyleDto>>;

internal sealed class GetStyleByIdQueryHandler
    : IRequestHandler<GetStyleByIdQuery, ApiResponse<StyleDto>>
{
    private readonly IRepository<Domain.Entities.Style> _repo;

    public GetStyleByIdQueryHandler(IRepository<Domain.Entities.Style> repo) => _repo = repo;

    public async Task<ApiResponse<StyleDto>> Handle(GetStyleByIdQuery request, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
            .Select(s => new StyleDto(
                s.Id, s.Code, s.StyleName,
                s.BuyerId, s.Buyer.Name,
                s.ProductId, s.Product != null ? s.Product.Name : null,
                s.BuyerStyleRef, s.Season, s.Status.ToString(),
                s.Description, s.Notes, s.IsActive))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? ApiResponse<StyleDto>.Fail("Style not found.")
            : ApiResponse<StyleDto>.Ok(dto);
    }
}
