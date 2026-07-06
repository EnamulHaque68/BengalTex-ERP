using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Expenses.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Expenses.Queries;

public sealed record GetExpensesQuery(
    PagedQueryParameters Parameters,
    int? CategoryId = null,
    string? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<ExpenseListItemDto>>>;

internal sealed class GetExpensesQueryHandler
    : IRequestHandler<GetExpensesQuery, ApiResponse<PagedResult<ExpenseListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    public GetExpensesQueryHandler(IRepository<Domain.Entities.Expense, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<ExpenseListItemDto>>> Handle(
        GetExpensesQuery request, CancellationToken ct)
    {
        var query = _repo.Query();
        if (request.CategoryId.HasValue) query = query.Where(e => e.ExpenseCategoryId == request.CategoryId.Value);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ExpenseStatus>(request.Status, out var st))
            query = query.Where(e => e.Status == st);
        if (request.FromDate.HasValue) query = query.Where(e => e.ExpenseDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(e => e.ExpenseDate <= request.ToDate.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(e => e.Code.Contains(search) ||
                (e.Payee != null && e.Payee.Contains(search)) ||
                (e.Description != null && e.Description.Contains(search)));

        query = query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Id);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(e => new ExpenseListItemDto(
                e.Id, e.Code, e.ExpenseDate, e.ExpenseCategory.Name, e.Amount,
                e.PaymentMethod.ToString(), e.Payee, e.Status.ToString()))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<ExpenseListItemDto>>.Ok(
            PagedResult<ExpenseListItemDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, total));
    }
}

public sealed record GetExpenseByIdQuery(long Id) : IRequest<ApiResponse<ExpenseDto>>;

internal sealed class GetExpenseByIdQueryHandler : IRequestHandler<GetExpenseByIdQuery, ApiResponse<ExpenseDto>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    public GetExpenseByIdQueryHandler(IRepository<Domain.Entities.Expense, long> repo) => _repo = repo;

    public async Task<ApiResponse<ExpenseDto>> Handle(GetExpenseByIdQuery request, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(e => e.Id == request.Id)
            .Select(e => new ExpenseDto(
                e.Id, e.Code, e.ExpenseDate, e.ExpenseCategoryId, e.ExpenseCategory.Name,
                e.Amount, e.PaymentMethod.ToString(), e.Payee, e.ReferenceNumber, e.Description,
                e.Status.ToString(), e.ApprovedAt, e.ApprovedBy,
                e.CostCenterId, e.CostCenter != null ? e.CostCenter.Name : null))
            .FirstOrDefaultAsync(ct);
        return dto is null ? ApiResponse<ExpenseDto>.Fail("Expense not found.") : ApiResponse<ExpenseDto>.Ok(dto);
    }
}

/// <summary>Approved-expense totals by category over a period (monthly expense summary).</summary>
public sealed record GetExpenseSummaryQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<ExpenseSummaryDto>>;

internal sealed class GetExpenseSummaryQueryHandler
    : IRequestHandler<GetExpenseSummaryQuery, ApiResponse<ExpenseSummaryDto>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    public GetExpenseSummaryQueryHandler(IRepository<Domain.Entities.Expense, long> repo) => _repo = repo;

    public async Task<ApiResponse<ExpenseSummaryDto>> Handle(GetExpenseSummaryQuery request, CancellationToken ct)
    {
        var rows = await _repo.Query()
            .Where(e => e.Status == ExpenseStatus.Approved
                     && e.ExpenseDate >= request.FromDate && e.ExpenseDate <= request.ToDate)
            .GroupBy(e => new { e.ExpenseCategoryId, e.ExpenseCategory.Name })
            .Select(g => new ExpenseSummaryRowDto(g.Key.ExpenseCategoryId, g.Key.Name, g.Sum(x => x.Amount), g.Count()))
            .ToListAsync(ct);

        rows = rows.OrderByDescending(r => r.Amount).ToList();
        return ApiResponse<ExpenseSummaryDto>.Ok(new ExpenseSummaryDto(
            request.FromDate, request.ToDate, rows, rows.Sum(r => r.Amount)));
    }
}
