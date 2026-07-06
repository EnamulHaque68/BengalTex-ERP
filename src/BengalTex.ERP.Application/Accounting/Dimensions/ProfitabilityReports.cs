using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Dimensions;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record ProfitabilityRowDto(
    int? DimensionId, string DimensionName,
    decimal Revenue, decimal Cogs, decimal GrossProfit, decimal MarginPercent);

public sealed record ProfitabilityReportDto(
    DateOnly FromDate, DateOnly ToDate, string Dimension,
    IReadOnlyList<ProfitabilityRowDto> Rows,
    decimal TotalRevenue, decimal TotalCogs, decimal TotalGrossProfit);

public sealed record CostCenterStatementRowDto(
    int? CostCenterId, string CostCenterName, decimal Income, decimal Expense, decimal Net);

public sealed record CostCenterStatementDto(
    DateOnly FromDate, DateOnly ToDate, IReadOnlyList<CostCenterStatementRowDto> Rows);

// ═══════════════════════════ Which dimension ═══════════════════════════

public enum ProfitDimension { Buyer, Style, Order }

/// <summary>
/// Phase A3 — revenue − COGS gross margin grouped by a business dimension (buyer / style / order),
/// read from posted, dimensioned journal lines. Revenue = Income-account credits (net of debits);
/// COGS = the COGS account (5100) debits. Closing vouchers are excluded (they zero P&amp;L).
/// </summary>
public sealed record GetProfitabilityReportQuery(ProfitDimension Dimension, DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<ProfitabilityReportDto>>;

internal sealed class GetProfitabilityReportQueryHandler
    : IRequestHandler<GetProfitabilityReportQuery, ApiResponse<ProfitabilityReportDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Style> _styleRepo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;

    public GetProfitabilityReportQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Style> styleRepo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo)
    {
        _lineRepo = lineRepo; _customerRepo = customerRepo; _styleRepo = styleRepo; _soRepo = soRepo;
    }

    private sealed record Agg(long Key, decimal Revenue, decimal Cogs);

    public async Task<ApiResponse<ProfitabilityReportDto>> Handle(GetProfitabilityReportQuery q, CancellationToken ct)
    {
        var cogsCode = Accounting.LedgerAccounts.CostOfGoodsSold;

        // Dimensioned P&L lines in range: Income (revenue, credit-normal) + COGS (debit-normal).
        var baseQuery = _lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.VoucherType != VoucherType.Closing
                     && l.JournalEntry.EntryDate >= q.FromDate
                     && l.JournalEntry.EntryDate <= q.ToDate
                     && (l.Account.AccountType == AccountType.Income || l.Account.Code == cogsCode));

        List<Agg> rows;
        Dictionary<long, string> names;

        if (q.Dimension == ProfitDimension.Order)
        {
            rows = (await baseQuery.Where(l => l.SalesOrderId != null)
                .GroupBy(l => l.SalesOrderId!.Value)
                .Select(g => new
                {
                    Key = g.Key,
                    Revenue = g.Where(x => x.Account.AccountType == AccountType.Income).Sum(x => x.Credit - x.Debit),
                    Cogs = g.Where(x => x.Account.Code == cogsCode).Sum(x => x.Debit - x.Credit)
                }).ToListAsync(ct))
                .Select(x => new Agg(x.Key, x.Revenue, x.Cogs)).ToList();
            var ids = rows.Select(r => r.Key).ToList();
            names = await _soRepo.Query().AsNoTracking().Where(s => ids.Contains(s.Id))
                .ToDictionaryAsync(s => (long)s.Id, s => s.Code, ct);
        }
        else if (q.Dimension == ProfitDimension.Buyer)
        {
            rows = (await baseQuery.Where(l => l.BuyerId != null)
                .GroupBy(l => l.BuyerId!.Value)
                .Select(g => new
                {
                    Key = g.Key,
                    Revenue = g.Where(x => x.Account.AccountType == AccountType.Income).Sum(x => x.Credit - x.Debit),
                    Cogs = g.Where(x => x.Account.Code == cogsCode).Sum(x => x.Debit - x.Credit)
                }).ToListAsync(ct))
                .Select(x => new Agg(x.Key, x.Revenue, x.Cogs)).ToList();
            var ids = rows.Select(r => (int)r.Key).ToList();
            names = await _customerRepo.Query().AsNoTracking().Where(c => ids.Contains(c.Id))
                .ToDictionaryAsync(c => (long)c.Id, c => c.Name, ct);
        }
        else // Style
        {
            rows = (await baseQuery.Where(l => l.StyleId != null)
                .GroupBy(l => l.StyleId!.Value)
                .Select(g => new
                {
                    Key = g.Key,
                    Revenue = g.Where(x => x.Account.AccountType == AccountType.Income).Sum(x => x.Credit - x.Debit),
                    Cogs = g.Where(x => x.Account.Code == cogsCode).Sum(x => x.Debit - x.Credit)
                }).ToListAsync(ct))
                .Select(x => new Agg(x.Key, x.Revenue, x.Cogs)).ToList();
            var ids = rows.Select(r => (int)r.Key).ToList();
            names = await _styleRepo.Query().AsNoTracking().Where(s => ids.Contains(s.Id))
                .ToDictionaryAsync(s => (long)s.Id, s => s.Code + " — " + s.StyleName, ct);
        }

        var dtoRows = rows.Select(r =>
        {
            var gp = r.Revenue - r.Cogs;
            var margin = r.Revenue != 0m ? Math.Round(gp / r.Revenue * 100m, 2) : 0m;
            var name = names.TryGetValue(r.Key, out var n) ? n : "(unassigned)";
            return new ProfitabilityRowDto((int)r.Key, name, Math.Round(r.Revenue, 2), Math.Round(r.Cogs, 2), Math.Round(gp, 2), margin);
        }).OrderByDescending(r => r.GrossProfit).ToList();

        return ApiResponse<ProfitabilityReportDto>.Ok(new ProfitabilityReportDto(
            q.FromDate, q.ToDate, q.Dimension.ToString(), dtoRows,
            dtoRows.Sum(r => r.Revenue), dtoRows.Sum(r => r.Cogs), dtoRows.Sum(r => r.GrossProfit)));
    }
}

// ═══════════════════════════ Cost-center statement ═══════════════════════════

public sealed record GetCostCenterStatementQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<CostCenterStatementDto>>;

internal sealed class GetCostCenterStatementQueryHandler
    : IRequestHandler<GetCostCenterStatementQuery, ApiResponse<CostCenterStatementDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.CostCenter> _ccRepo;

    public GetCostCenterStatementQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo, IRepository<Domain.Entities.CostCenter> ccRepo)
    {
        _lineRepo = lineRepo; _ccRepo = ccRepo;
    }

    public async Task<ApiResponse<CostCenterStatementDto>> Handle(GetCostCenterStatementQuery q, CancellationToken ct)
    {
        var rows = await _lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.VoucherType != VoucherType.Closing
                     && l.JournalEntry.EntryDate >= q.FromDate
                     && l.JournalEntry.EntryDate <= q.ToDate
                     && l.CostCenterId != null
                     && (l.Account.AccountType == AccountType.Income || l.Account.AccountType == AccountType.Expense))
            .GroupBy(l => l.CostCenterId)
            .Select(g => new
            {
                CostCenterId = g.Key,
                Income = g.Where(x => x.Account.AccountType == AccountType.Income).Sum(x => x.Credit - x.Debit),
                Expense = g.Where(x => x.Account.AccountType == AccountType.Expense).Sum(x => x.Debit - x.Credit)
            })
            .ToListAsync(ct);

        var ids = rows.Where(r => r.CostCenterId.HasValue).Select(r => r.CostCenterId!.Value).ToList();
        var names = await _ccRepo.Query().AsNoTracking().Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => $"{c.Code} — {c.Name}", ct);

        var dtoRows = rows.Select(r => new CostCenterStatementRowDto(
                r.CostCenterId,
                r.CostCenterId.HasValue && names.TryGetValue(r.CostCenterId.Value, out var n) ? n : "(unassigned)",
                Math.Round(r.Income, 2), Math.Round(r.Expense, 2), Math.Round(r.Income - r.Expense, 2)))
            .OrderBy(r => r.CostCenterName).ToList();

        return ApiResponse<CostCenterStatementDto>.Ok(new CostCenterStatementDto(q.FromDate, q.ToDate, dtoRows));
    }
}
