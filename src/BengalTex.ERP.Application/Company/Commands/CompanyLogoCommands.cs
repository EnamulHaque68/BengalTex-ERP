using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Company.Commands;

/// <summary>Sets (or clears, when null) the storage path of the uploaded company logo on the singleton company.</summary>
public sealed record SetCompanyLogoCommand(string? StoragePath) : IRequest<ApiResponse<string>>;

internal sealed class SetCompanyLogoCommandHandler : IRequestHandler<SetCompanyLogoCommand, ApiResponse<string>>
{
    private readonly IRepository<Domain.Entities.Company> _repo;
    private readonly IUnitOfWork _uow;

    public SetCompanyLogoCommandHandler(IRepository<Domain.Entities.Company> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<string>> Handle(SetCompanyLogoCommand cmd, CancellationToken ct)
    {
        var company = await _repo.Query().OrderBy(c => c.Id).FirstOrDefaultAsync(ct);
        if (company is null) return ApiResponse<string>.Fail("Set up the company profile first, then upload a logo.");

        company.LogoUrl = string.IsNullOrWhiteSpace(cmd.StoragePath) ? null : cmd.StoragePath;
        _repo.Update(company);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<string>.Ok(company.LogoUrl ?? "", cmd.StoragePath is null ? "Logo removed." : "Logo updated.");
    }
}

/// <summary>Returns the storage path of the company logo (null if none), for serving the image.</summary>
public sealed record GetCompanyLogoPathQuery : IRequest<string?>;

internal sealed class GetCompanyLogoPathQueryHandler : IRequestHandler<GetCompanyLogoPathQuery, string?>
{
    private readonly IRepository<Domain.Entities.Company> _repo;
    public GetCompanyLogoPathQueryHandler(IRepository<Domain.Entities.Company> repo) => _repo = repo;

    public async Task<string?> Handle(GetCompanyLogoPathQuery req, CancellationToken ct)
        => (await _repo.Query().AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(ct))?.LogoUrl;
}
