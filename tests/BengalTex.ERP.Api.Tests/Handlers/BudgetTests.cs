using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.Budgeting;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A7a — annual budgeting. A budget holds 12 FY-relative monthly amounts per account; the
/// Budget-vs-Actual report compares the budgeted range total to posted GL movement (natural sign).
/// </summary>
public class BudgetTests
{
    private static (long BudgetId, ApplicationDbContext Ctx) SeededBudget(ApplicationDbContext ctx)
    {
        ctx.Accounts.Add(new Account { Id = 1, Code = "5400", Name = "Admin Expense", AccountType = AccountType.Expense });
        ctx.FinancialYears.Add(new FinancialYear { Id = 1, Code = "FY2026", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) });
        ctx.SaveChanges();

        var create = new CreateBudgetCommandHandler(
            new Repository<Budget, long>(ctx), new Repository<FinancialYear>(ctx), new UnitOfWork(ctx), TestHarness.Numbering().Object)
            .Handle(new CreateBudgetCommand(1, "FY2026 Operating Budget", null), default).Result;
        var budgetId = create.Data;

        new SetBudgetLinesCommandHandler(new Repository<Budget, long>(ctx), new Repository<BudgetLine, long>(ctx), new UnitOfWork(ctx))
            .Handle(new SetBudgetLinesCommand(budgetId, new[]
            {
                new BudgetLineInput(1, null, 1_000m, 1_000m, 1_000m, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            }), default).Wait();

        return (budgetId, ctx);
    }

    [Fact]
    public async Task Budget_detail_totals_the_monthly_amounts()
    {
        await using var ctx = TestHarness.NewContext();
        var (budgetId, _) = SeededBudget(ctx);

        var res = await new GetBudgetByIdQueryHandler(new Repository<Budget, long>(ctx))
            .Handle(new GetBudgetByIdQuery(budgetId), default);

        res.Success.Should().BeTrue();
        res.Data!.Lines.Should().HaveCount(1);
        res.Data.Lines[0].Total.Should().Be(3_000m);   // 1,000 × 3 months
    }

    [Fact]
    public async Task Variance_compares_budget_range_to_posted_actual()
    {
        await using var ctx = TestHarness.NewContext();
        var (budgetId, _) = SeededBudget(ctx);

        // Actual admin expense in Jan: Dr 5400 1,800.
        var je = new JournalEntry { Code = "JV1", EntryDate = new DateOnly(2026, 1, 15), Status = JournalEntryStatus.Posted, PostedAt = DateTimeOffset.UtcNow, PostedBy = "t" };
        je.Lines.Add(new JournalEntryLine { AccountId = 1, Debit = 1_800m, Credit = 0m, SortOrder = 0 });
        ctx.JournalEntries.Add(je);
        ctx.SaveChanges();

        var res = await new GetBudgetVarianceQueryHandler(new Repository<Budget, long>(ctx), new Repository<JournalEntryLine, long>(ctx))
            .Handle(new GetBudgetVarianceQuery(budgetId, 1, 2), default);   // Jan–Feb

        res.Success.Should().BeTrue();
        var row = res.Data!.Rows.Single(r => r.AccountCode == "5400");
        row.Budget.Should().Be(2_000m);      // M1 + M2
        row.Actual.Should().Be(1_800m);      // Dr − Cr (expense natural)
        row.Variance.Should().Be(-200m);     // actual − budget (under budget)
        res.Data.TotalVariance.Should().Be(-200m);
    }

    [Fact]
    public async Task Only_draft_budgets_can_be_edited_or_deleted()
    {
        await using var ctx = TestHarness.NewContext();
        var (budgetId, _) = SeededBudget(ctx);

        (await new ApproveBudgetCommandHandler(new Repository<Budget, long>(ctx), new UnitOfWork(ctx))
            .Handle(new ApproveBudgetCommand(budgetId), default)).Success.Should().BeTrue();

        // Approved → line edit + delete both refused.
        (await new SetBudgetLinesCommandHandler(new Repository<Budget, long>(ctx), new Repository<BudgetLine, long>(ctx), new UnitOfWork(ctx))
            .Handle(new SetBudgetLinesCommand(budgetId, Array.Empty<BudgetLineInput>()), default)).Success.Should().BeFalse();
        (await new DeleteBudgetCommandHandler(new Repository<Budget, long>(ctx), new UnitOfWork(ctx))
            .Handle(new DeleteBudgetCommand(budgetId), default)).Success.Should().BeFalse();
    }
}
