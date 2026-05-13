using BengalTex.ERP.Application.Company.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Company.Queries;

public sealed record GetCompanyQuery : IRequest<ApiResponse<CompanyDto>>;

internal sealed class GetCompanyQueryHandler : IRequestHandler<GetCompanyQuery, ApiResponse<CompanyDto>>
{
    private readonly IRepository<Domain.Entities.Company> _repo;
    private readonly IMapper _mapper;

    public GetCompanyQueryHandler(IRepository<Domain.Entities.Company> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CompanyDto>> Handle(GetCompanyQuery request, CancellationToken cancellationToken)
    {
        var company = await _repo.Query().FirstOrDefaultAsync(cancellationToken);

        if (company is null)
            return ApiResponse<CompanyDto>.Fail("Company profile not found.");

        return ApiResponse<CompanyDto>.Ok(_mapper.Map<CompanyDto>(company));
    }
}
