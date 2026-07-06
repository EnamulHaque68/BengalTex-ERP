using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>The full chart of accounts (flat, ordered by Code). Optional type / active / search filters.</summary>
public sealed record GetAccountsQuery(
    string? AccountType = null,
    bool IncludeInactive = false,
    bool? PostableOnly = null,     // true → exclude group/header accounts (for journal pickers)
    string? Search = null
) : IRequest<ApiResponse<IReadOnlyList<AccountDto>>>;

internal sealed class GetAccountsQueryHandler
    : IRequestHandler<GetAccountsQuery, ApiResponse<IReadOnlyList<AccountDto>>>
{
    private readonly IRepository<Domain.Entities.Account> _repo;

    public GetAccountsQueryHandler(IRepository<Domain.Entities.Account> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<AccountDto>>> Handle(
        GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!request.IncludeInactive) query = query.Where(a => a.IsActive);
        if (request.PostableOnly == true) query = query.Where(a => !a.IsGroup);

        if (!string.IsNullOrEmpty(request.AccountType)
            && Enum.TryParse<AccountType>(request.AccountType, out var type))
            query = query.Where(a => a.AccountType == type);

        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => a.Code.Contains(search) || a.Name.Contains(search));

        var rows = await query
            .OrderBy(a => a.Code)
            .Select(a => new
            {
                a.Id, a.Code, a.Name, a.AccountType, a.IsGroup,
                a.ParentAccountId,
                ParentName = a.ParentAccount != null ? a.ParentAccount.Name : null,
                a.IsSystem, a.IsActive, a.Description, a.RequiresCostCenter
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(a => new AccountDto(
            a.Id, a.Code, a.Name, a.AccountType.ToString(),
            AccountingMapping.NormalBalanceOf(a.AccountType),
            a.IsGroup, a.ParentAccountId, a.ParentName,
            a.IsSystem, a.IsActive, a.Description, a.RequiresCostCenter)).ToList();

        return ApiResponse<IReadOnlyList<AccountDto>>.Ok(items);
    }
}

public sealed record GetAccountByIdQuery(int Id) : IRequest<ApiResponse<AccountDto>>;

internal sealed class GetAccountByIdQueryHandler
    : IRequestHandler<GetAccountByIdQuery, ApiResponse<AccountDto>>
{
    private readonly IRepository<Domain.Entities.Account> _repo;

    public GetAccountByIdQueryHandler(IRepository<Domain.Entities.Account> repo) => _repo = repo;

    public async Task<ApiResponse<AccountDto>> Handle(
        GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var a = await _repo.Query()
            .Where(x => x.Id == request.Id)
            .Select(x => new
            {
                x.Id, x.Code, x.Name, x.AccountType, x.IsGroup,
                x.ParentAccountId,
                ParentName = x.ParentAccount != null ? x.ParentAccount.Name : null,
                x.IsSystem, x.IsActive, x.Description, x.RequiresCostCenter
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (a is null) return ApiResponse<AccountDto>.Fail("Account not found.");

        return ApiResponse<AccountDto>.Ok(new AccountDto(
            a.Id, a.Code, a.Name, a.AccountType.ToString(),
            AccountingMapping.NormalBalanceOf(a.AccountType),
            a.IsGroup, a.ParentAccountId, a.ParentName,
            a.IsSystem, a.IsActive, a.Description, a.RequiresCostCenter));
    }
}
