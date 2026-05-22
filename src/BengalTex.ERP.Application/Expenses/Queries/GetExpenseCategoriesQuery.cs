using BengalTex.ERP.Application.Expenses.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Expenses.Queries;

public sealed record GetExpenseCategoriesQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>;

internal sealed class GetExpenseCategoriesQueryHandler
    : IRequestHandler<GetExpenseCategoriesQuery, ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>
{
    private readonly IRepository<ExpenseCategory> _repo;

    public GetExpenseCategoriesQueryHandler(IRepository<ExpenseCategory> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>> Handle(
        GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();
        if (!request.IncludeInactive) query = query.Where(c => c.IsActive);

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryDto(
                c.Id, c.Name, c.LedgerAccountId,
                c.LedgerAccount != null ? c.LedgerAccount.Code : null,
                c.LedgerAccount != null ? c.LedgerAccount.Name : null,
                c.IsActive, c.Description))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<ExpenseCategoryDto>>.Ok(items);
    }
}
