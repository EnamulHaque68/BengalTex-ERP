using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Budgeting;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record BudgetDto(
    long Id, string Code, int FinancialYearId, string FinancialYearCode, string Name, string Status, int LineCount, decimal AnnualTotal);

public sealed record BudgetLineDto(
    long Id, int AccountId, string AccountCode, string AccountName, int? CostCenterId, string? CostCenterName,
    decimal M1, decimal M2, decimal M3, decimal M4, decimal M5, decimal M6,
    decimal M7, decimal M8, decimal M9, decimal M10, decimal M11, decimal M12, decimal Total);

public sealed record BudgetDetailDto(
    long Id, string Code, int FinancialYearId, string FinancialYearCode, string Name, string Status, string? Notes,
    IReadOnlyList<BudgetLineDto> Lines);

public sealed record BudgetVarianceRowDto(
    int AccountId, string AccountCode, string AccountName, string AccountType,
    decimal Budget, decimal Actual, decimal Variance, decimal VariancePct);

public sealed record BudgetVarianceReportDto(
    long BudgetId, string BudgetCode, int FromMonth, int ToMonth,
    IReadOnlyList<BudgetVarianceRowDto> Rows, decimal TotalBudget, decimal TotalActual, decimal TotalVariance);

internal static class BudgetMath
{
    public static decimal LineRangeTotal(BudgetLine l, int from, int to)
    {
        var months = new[] { 0m, l.M1, l.M2, l.M3, l.M4, l.M5, l.M6, l.M7, l.M8, l.M9, l.M10, l.M11, l.M12 };
        decimal sum = 0m;
        for (var m = from; m <= to; m++) sum += months[m];
        return sum;
    }
    public static decimal LineTotal(BudgetLine l) => LineRangeTotal(l, 1, 12);
}

// ═══════════════════════════ Queries ═══════════════════════════

public sealed record GetBudgetsQuery(int? FinancialYearId = null) : IRequest<ApiResponse<IReadOnlyList<BudgetDto>>>;

internal sealed class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, ApiResponse<IReadOnlyList<BudgetDto>>>
{
    private readonly IRepository<Budget, long> _repo;
    public GetBudgetsQueryHandler(IRepository<Budget, long> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<BudgetDto>>> Handle(GetBudgetsQuery q, CancellationToken ct)
    {
        IQueryable<Budget> query = _repo.Query().AsNoTracking()
            .Include(b => b.Lines).Include(b => b.FinancialYear);
        if (q.FinancialYearId.HasValue) query = query.Where(b => b.FinancialYearId == q.FinancialYearId.Value);

        var budgets = await query.OrderByDescending(b => b.Id).ToListAsync(ct);
        var rows = budgets.Select(b => new BudgetDto(
            b.Id, b.Code, b.FinancialYearId, b.FinancialYear.Code, b.Name, b.Status.ToString(),
            b.Lines.Count, Math.Round(b.Lines.Sum(BudgetMath.LineTotal), 2))).ToList();
        return ApiResponse<IReadOnlyList<BudgetDto>>.Ok(rows);
    }
}

public sealed record GetBudgetByIdQuery(long Id) : IRequest<ApiResponse<BudgetDetailDto>>;

internal sealed class GetBudgetByIdQueryHandler : IRequestHandler<GetBudgetByIdQuery, ApiResponse<BudgetDetailDto>>
{
    private readonly IRepository<Budget, long> _repo;
    public GetBudgetByIdQueryHandler(IRepository<Budget, long> repo) => _repo = repo;

    public async Task<ApiResponse<BudgetDetailDto>> Handle(GetBudgetByIdQuery q, CancellationToken ct)
    {
        var b = await _repo.Query().AsNoTracking()
            .Include(x => x.FinancialYear)
            .Include(x => x.Lines).ThenInclude(l => l.Account)
            .Include(x => x.Lines).ThenInclude(l => l.CostCenter)
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);
        if (b is null) return ApiResponse<BudgetDetailDto>.Fail("Budget not found.");

        var lines = b.Lines.OrderBy(l => l.Account.Code).Select(l => new BudgetLineDto(
            l.Id, l.AccountId, l.Account.Code, l.Account.Name, l.CostCenterId, l.CostCenter != null ? l.CostCenter.Name : null,
            l.M1, l.M2, l.M3, l.M4, l.M5, l.M6, l.M7, l.M8, l.M9, l.M10, l.M11, l.M12, BudgetMath.LineTotal(l))).ToList();

        return ApiResponse<BudgetDetailDto>.Ok(new BudgetDetailDto(
            b.Id, b.Code, b.FinancialYearId, b.FinancialYear.Code, b.Name, b.Status.ToString(), b.Notes, lines));
    }
}

// ═══════════════════════════ Create / edit lines / approve / delete ═══════════════════════════

public sealed record CreateBudgetCommand(int FinancialYearId, string Name, string? Notes) : IRequest<ApiResponse<long>>;

public sealed class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(x => x.FinancialYearId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, ApiResponse<long>>
{
    private readonly IRepository<Budget, long> _repo;
    private readonly IRepository<FinancialYear> _fyRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateBudgetCommandHandler(IRepository<Budget, long> repo, IRepository<FinancialYear> fyRepo, IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _fyRepo = fyRepo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateBudgetCommand cmd, CancellationToken ct)
    {
        if (!await _fyRepo.Query().AnyAsync(f => f.Id == cmd.FinancialYearId, ct))
            return ApiResponse<long>.Fail("Financial year not found.");

        var code = await _numbering.NextAsync("BUD", null, ct);
        var entity = new Budget
        {
            Code = code, FinancialYearId = cmd.FinancialYearId, Name = cmd.Name.Trim(),
            Status = BudgetStatus.Draft, Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Budget created.");
    }
}

public sealed record BudgetLineInput(
    int AccountId, int? CostCenterId,
    decimal M1, decimal M2, decimal M3, decimal M4, decimal M5, decimal M6,
    decimal M7, decimal M8, decimal M9, decimal M10, decimal M11, decimal M12);

public sealed record SetBudgetLinesCommand(long BudgetId, IReadOnlyList<BudgetLineInput> Lines) : IRequest<ApiResponse>;

internal sealed class SetBudgetLinesCommandHandler : IRequestHandler<SetBudgetLinesCommand, ApiResponse>
{
    private readonly IRepository<Budget, long> _repo;
    private readonly IRepository<BudgetLine, long> _lineRepo;
    private readonly IUnitOfWork _uow;

    public SetBudgetLinesCommandHandler(IRepository<Budget, long> repo, IRepository<BudgetLine, long> lineRepo, IUnitOfWork uow)
    { _repo = repo; _lineRepo = lineRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(SetBudgetLinesCommand cmd, CancellationToken ct)
    {
        var b = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.BudgetId, ct);
        if (b is null) return ApiResponse.Fail("Budget not found.");
        if (b.Status != BudgetStatus.Draft) return ApiResponse.Fail("Only a Draft budget can be edited.");

        foreach (var existing in b.Lines.ToList()) _lineRepo.Remove(existing);
        foreach (var i in cmd.Lines)
            await _lineRepo.AddAsync(new BudgetLine
            {
                BudgetId = b.Id, AccountId = i.AccountId, CostCenterId = i.CostCenterId,
                M1 = i.M1, M2 = i.M2, M3 = i.M3, M4 = i.M4, M5 = i.M5, M6 = i.M6,
                M7 = i.M7, M8 = i.M8, M9 = i.M9, M10 = i.M10, M11 = i.M11, M12 = i.M12
            }, ct);

        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"{cmd.Lines.Count} budget line(s) saved.");
    }
}

public sealed record ApproveBudgetCommand(long Id) : IRequest<ApiResponse>;

internal sealed class ApproveBudgetCommandHandler : IRequestHandler<ApproveBudgetCommand, ApiResponse>
{
    private readonly IRepository<Budget, long> _repo;
    private readonly IUnitOfWork _uow;
    public ApproveBudgetCommandHandler(IRepository<Budget, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(ApproveBudgetCommand cmd, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) return ApiResponse.Fail("Budget not found.");
        if (b.Status != BudgetStatus.Draft) return ApiResponse.Fail($"Budget is already {b.Status}.");
        b.Status = BudgetStatus.Approved;
        _repo.Update(b);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Budget approved.");
    }
}

public sealed record DeleteBudgetCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand, ApiResponse>
{
    private readonly IRepository<Budget, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteBudgetCommandHandler(IRepository<Budget, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteBudgetCommand cmd, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) return ApiResponse.Fail("Budget not found.");
        if (b.Status != BudgetStatus.Draft) return ApiResponse.Fail("Only a Draft budget can be deleted.");
        _repo.Remove(b);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Budget deleted.");
    }
}

// ═══════════════════════════ Budget vs Actual variance ═══════════════════════════

public sealed record GetBudgetVarianceQuery(long BudgetId, int FromMonth = 1, int ToMonth = 12, int? CostCenterId = null)
    : IRequest<ApiResponse<BudgetVarianceReportDto>>;

internal sealed class GetBudgetVarianceQueryHandler : IRequestHandler<GetBudgetVarianceQuery, ApiResponse<BudgetVarianceReportDto>>
{
    private readonly IRepository<Budget, long> _repo;
    private readonly IRepository<JournalEntryLine, long> _lineRepo;

    public GetBudgetVarianceQueryHandler(IRepository<Budget, long> repo, IRepository<JournalEntryLine, long> lineRepo)
    { _repo = repo; _lineRepo = lineRepo; }

    public async Task<ApiResponse<BudgetVarianceReportDto>> Handle(GetBudgetVarianceQuery q, CancellationToken ct)
    {
        var from = Math.Clamp(q.FromMonth, 1, 12);
        var to = Math.Clamp(q.ToMonth, from, 12);

        var b = await _repo.Query().AsNoTracking()
            .Include(x => x.FinancialYear)
            .Include(x => x.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(x => x.Id == q.BudgetId, ct);
        if (b is null) return ApiResponse<BudgetVarianceReportDto>.Fail("Budget not found.");

        var lines = q.CostCenterId is int cc ? b.Lines.Where(l => l.CostCenterId == cc).ToList() : b.Lines.ToList();

        // Budget per account over the month range.
        var budgetByAccount = lines
            .GroupBy(l => new { l.AccountId, l.Account.Code, l.Account.Name, l.Account.AccountType })
            .Select(g => new { g.Key.AccountId, g.Key.Code, g.Key.Name, g.Key.AccountType, Budget = g.Sum(x => BudgetMath.LineRangeTotal(x, from, to)) })
            .ToList();
        var accountIds = budgetByAccount.Select(x => x.AccountId).ToHashSet();

        // Actual GL movement for those accounts over the FY month-range dates.
        var start = b.FinancialYear.StartDate;
        var dateFrom = start.AddMonths(from - 1);
        var dateTo = start.AddMonths(to).AddDays(-1);

        var actualsQuery = _lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate >= dateFrom && l.JournalEntry.EntryDate <= dateTo
                     && accountIds.Contains(l.AccountId));
        if (q.CostCenterId is int ccId) actualsQuery = actualsQuery.Where(l => l.CostCenterId == ccId);

        var actuals = (await actualsQuery
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct))
            .ToDictionary(x => x.AccountId, x => new { x.Debit, x.Credit });

        var rows = new List<BudgetVarianceRowDto>();
        foreach (var ba in budgetByAccount.OrderBy(x => x.Code))
        {
            decimal actual = 0m;
            if (actuals.TryGetValue(ba.AccountId, out var a))
                actual = ba.AccountType is AccountType.Income or AccountType.Liability or AccountType.Equity
                    ? a.Credit - a.Debit    // credit-normal
                    : a.Debit - a.Credit;   // debit-normal (Asset / Expense)
            actual = Math.Round(actual, 2);
            var budget = Math.Round(ba.Budget, 2);
            var variance = Math.Round(actual - budget, 2);
            var pct = budget != 0m ? Math.Round(variance / budget * 100m, 1) : 0m;
            rows.Add(new BudgetVarianceRowDto(ba.AccountId, ba.Code, ba.Name, ba.AccountType.ToString(), budget, actual, variance, pct));
        }

        return ApiResponse<BudgetVarianceReportDto>.Ok(new BudgetVarianceReportDto(
            b.Id, b.Code, from, to, rows,
            Math.Round(rows.Sum(r => r.Budget), 2), Math.Round(rows.Sum(r => r.Actual), 2), Math.Round(rows.Sum(r => r.Variance), 2)));
    }
}
