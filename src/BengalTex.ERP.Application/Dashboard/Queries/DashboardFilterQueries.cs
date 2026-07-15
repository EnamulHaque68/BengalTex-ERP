using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Dashboard.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Dashboard.Queries;

// ═══════════════════════════ Expense breakdown for a date range ═══════════════════════════
//
// Additive companion to the snapshot's this-month expense breakdown — same grouping (posted
// Expense-account movement by account name, top 6 + Other), but for an arbitrary [from,to] so the
// Expense Breakdown widget's period dropdown / header date-range can drive it. Permission-gated
// exactly like the snapshot section.

public sealed record GetDashboardExpenseBreakdownQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<IReadOnlyList<ExpenseBreakdownItemDto>>>;

internal sealed class GetDashboardExpenseBreakdownQueryHandler
    : IRequestHandler<GetDashboardExpenseBreakdownQuery, ApiResponse<IReadOnlyList<ExpenseBreakdownItemDto>>>
{
    private readonly IRepository<JournalEntryLine, long> _jLineRepo;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardExpenseBreakdownQueryHandler(IRepository<JournalEntryLine, long> jLineRepo, ICurrentUserService currentUser)
    { _jLineRepo = jLineRepo; _currentUser = currentUser; }

    public async Task<ApiResponse<IReadOnlyList<ExpenseBreakdownItemDto>>> Handle(GetDashboardExpenseBreakdownQuery q, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(Permissions.Dashboard.ViewOwner) && !_currentUser.HasPermission(Permissions.Dashboard.ViewAccounts))
            return ApiResponse<IReadOnlyList<ExpenseBreakdownItemDto>>.Fail("Not permitted.");

        var expRows = await _jLineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.VoucherType != VoucherType.Closing
                     && l.JournalEntry.EntryDate >= q.FromDate && l.JournalEntry.EntryDate <= q.ToDate
                     && l.Account.AccountType == AccountType.Expense)
            .GroupBy(l => l.Account.Name)
            .Select(g => new { Name = g.Key, Amt = g.Sum(x => x.Debit - x.Credit) })
            .ToListAsync(ct);

        var ordered = expRows.Where(r => r.Amt > 0m).OrderByDescending(r => r.Amt).ToList();
        var top = ordered.Take(6).Select(r => new ExpenseBreakdownItemDto(r.Name, Math.Round(r.Amt, 2))).ToList();
        var otherSum = ordered.Skip(6).Sum(r => r.Amt);
        if (otherSum > 0m) top.Add(new ExpenseBreakdownItemDto("Other", Math.Round(otherSum, 2)));

        return ApiResponse<IReadOnlyList<ExpenseBreakdownItemDto>>.Ok(top);
    }
}

// ═══════════════════════════ Production overview for a date range ═══════════════════════════

public sealed record GetDashboardProductionOverviewQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<ProductionOverviewDto>>;

internal sealed class GetDashboardProductionOverviewQueryHandler
    : IRequestHandler<GetDashboardProductionOverviewQuery, ApiResponse<ProductionOverviewDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _prodRepo;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardProductionOverviewQueryHandler(IRepository<Domain.Entities.ProductionOrder, long> prodRepo, ICurrentUserService currentUser)
    { _prodRepo = prodRepo; _currentUser = currentUser; }

    public async Task<ApiResponse<ProductionOverviewDto>> Handle(GetDashboardProductionOverviewQuery q, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(Permissions.Dashboard.ViewOwner) && !_currentUser.HasPermission(Permissions.Dashboard.ViewProduction))
            return ApiResponse<ProductionOverviewDto>.Fail("Not permitted.");

        var completedQty = await _prodRepo.Query().AsNoTracking()
            .Where(p => p.Status == ProductionOrderStatus.Completed
                     && p.ActualEndDate != null && p.ActualEndDate >= q.FromDate && p.ActualEndDate <= q.ToDate)
            .SumAsync(p => (decimal?)p.Quantity, ct) ?? 0m;
        var inProgressQty = await _prodRepo.Query().AsNoTracking()
            .Where(p => p.Status == ProductionOrderStatus.InProgress)
            .SumAsync(p => (decimal?)p.Quantity, ct) ?? 0m;
        var target = completedQty + inProgressQty;

        return ApiResponse<ProductionOverviewDto>.Ok(new ProductionOverviewDto(
            Math.Round(target, 2), Math.Round(completedQty, 2),
            target > 0m ? Math.Round(completedQty / target * 100m, 1) : 0m));
    }
}
