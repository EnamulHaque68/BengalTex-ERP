using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Intelligence;

// ═══════════════════════════ I1 — Financial ratios / KPIs ═══════════════════════════

public sealed record FinancialKpisDto(
    DateOnly AsOfDate, DateOnly FromDate, DateOnly ToDate,
    // Balance-sheet values (as-of)
    decimal CurrentAssets, decimal CurrentLiabilities, decimal Inventory, decimal WorkingCapital,
    decimal TotalAssets, decimal TotalLiabilities, decimal TotalEquity,
    decimal AccountsReceivable, decimal AccountsPayable,
    // P&L values (period)
    decimal Revenue, decimal Cogs, decimal GrossProfit, decimal NetProfit,
    // Ratios
    decimal CurrentRatio, decimal QuickRatio, decimal DebtToEquity,
    decimal GrossMarginPct, decimal NetMarginPct, decimal ReturnOnAssetsPct,
    decimal InventoryTurnover, decimal Dso, decimal Dpo);

public sealed record GetFinancialKpisQuery(DateOnly? AsOfDate = null, DateOnly? FromDate = null, DateOnly? ToDate = null)
    : IRequest<ApiResponse<FinancialKpisDto>>;

internal sealed class GetFinancialKpisQueryHandler : IRequestHandler<GetFinancialKpisQuery, ApiResponse<FinancialKpisDto>>
{
    private readonly IMediator _mediator;
    public GetFinancialKpisQueryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<ApiResponse<FinancialKpisDto>> Handle(GetFinancialKpisQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = q.FromDate ?? new DateOnly(asOf.Year, 1, 1);
        var to = q.ToDate ?? asOf;

        var bsRes = await _mediator.Send(new GetBalanceSheetQuery(asOf), ct);
        var plRes = await _mediator.Send(new GetProfitAndLossQuery(from, to), ct);
        if (bsRes.Data is not { } bs || plRes.Data is not { } pl)
            return ApiResponse<FinancialKpisDto>.Fail("Could not compute the underlying statements.");

        decimal SumWhere(IEnumerable<StatementLineDto> lines, Func<StatementLineDto, bool> pred) =>
            lines.Where(pred).Sum(l => l.Amount);

        var currentAssets = SumWhere(bs.Assets, a => a.AccountCode.StartsWith("11"));
        var inventory = SumWhere(bs.Assets, a => a.AccountCode is "1140" or "1150" or "1160");
        var ar = SumWhere(bs.Assets, a => a.AccountCode == "1130");
        var currentLiab = SumWhere(bs.Liabilities, l => l.AccountCode.StartsWith("21"));
        var ap = SumWhere(bs.Liabilities, l => l.AccountCode == "2110");

        var revenue = pl.TotalIncome;
        var cogs = SumWhere(pl.Expenses, e => e.AccountCode == "5100");
        var grossProfit = revenue - cogs;
        var net = pl.NetProfit;

        var days = Math.Max(1, to.DayNumber - from.DayNumber + 1);

        decimal Div(decimal a, decimal b, int dp = 2) => b != 0m ? Math.Round(a / b, dp) : 0m;
        decimal Pct(decimal a, decimal b) => b != 0m ? Math.Round(a / b * 100m, 1) : 0m;

        var dto = new FinancialKpisDto(
            asOf, from, to,
            Math.Round(currentAssets, 2), Math.Round(currentLiab, 2), Math.Round(inventory, 2),
            Math.Round(currentAssets - currentLiab, 2),
            Math.Round(bs.TotalAssets, 2), Math.Round(bs.TotalLiabilities, 2), Math.Round(bs.TotalEquity, 2),
            Math.Round(ar, 2), Math.Round(ap, 2),
            Math.Round(revenue, 2), Math.Round(cogs, 2), Math.Round(grossProfit, 2), Math.Round(net, 2),
            Div(currentAssets, currentLiab), Div(currentAssets - inventory, currentLiab), Div(bs.TotalLiabilities, bs.TotalEquity),
            Pct(grossProfit, revenue), Pct(net, revenue), Pct(net, bs.TotalAssets),
            inventory != 0m ? Math.Round(cogs * 365m / days / inventory, 2) : 0m,
            revenue != 0m ? Math.Round(ar / revenue * days, 0) : 0m,
            cogs != 0m ? Math.Round(ap / cogs * days, 0) : 0m);

        return ApiResponse<FinancialKpisDto>.Ok(dto);
    }
}

// ═══════════════════════════ I2 — AR / AP aging ═══════════════════════════

public sealed record AgingRowDto(
    string Party, decimal Bucket0_30, decimal Bucket31_60, decimal Bucket61_90, decimal Bucket90Plus, decimal Total);

public sealed record AgingReportDto(
    string Kind, IReadOnlyList<AgingRowDto> Rows,
    decimal Total0_30, decimal Total31_60, decimal Total61_90, decimal Total90Plus, decimal GrandTotal);

public sealed record ArApAgingDto(DateOnly AsOfDate, AgingReportDto Receivables, AgingReportDto Payables);

public sealed record GetArApAgingQuery(DateOnly? AsOfDate = null) : IRequest<ApiResponse<ArApAgingDto>>;

internal sealed class GetArApAgingQueryHandler : IRequestHandler<GetArApAgingQuery, ApiResponse<ArApAgingDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _arRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _apRepo;

    public GetArApAgingQueryHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> arRepo, IRepository<Domain.Entities.SupplierInvoice, long> apRepo)
    {
        _arRepo = arRepo; _apRepo = apRepo;
    }

    private sealed record OpenItem(string Party, decimal OutstandingBdt, int AgeDays);

    private static AgingReportDto Build(string kind, IEnumerable<OpenItem> items)
    {
        var rows = items
            .GroupBy(i => i.Party)
            .Select(g =>
            {
                decimal b0 = 0, b1 = 0, b2 = 0, b3 = 0;
                foreach (var i in g)
                {
                    if (i.AgeDays <= 30) b0 += i.OutstandingBdt;
                    else if (i.AgeDays <= 60) b1 += i.OutstandingBdt;
                    else if (i.AgeDays <= 90) b2 += i.OutstandingBdt;
                    else b3 += i.OutstandingBdt;
                }
                return new AgingRowDto(g.Key, Math.Round(b0, 2), Math.Round(b1, 2), Math.Round(b2, 2), Math.Round(b3, 2),
                    Math.Round(b0 + b1 + b2 + b3, 2));
            })
            .Where(r => r.Total != 0m)
            .OrderByDescending(r => r.Total)
            .ToList();

        return new AgingReportDto(kind, rows,
            Math.Round(rows.Sum(r => r.Bucket0_30), 2), Math.Round(rows.Sum(r => r.Bucket31_60), 2),
            Math.Round(rows.Sum(r => r.Bucket61_90), 2), Math.Round(rows.Sum(r => r.Bucket90Plus), 2),
            Math.Round(rows.Sum(r => r.Total), 2));
    }

    public async Task<ApiResponse<ArApAgingDto>> Handle(GetArApAgingQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var arItems = (await _arRepo.Query().AsNoTracking()
            .Where(i => (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                     && i.InvoiceDate <= asOf && i.TotalAmount - i.AmountPaid > 0m)
            .Select(i => new { Party = i.Customer.Name, Outstanding = (i.TotalAmount - i.AmountPaid) * i.ExchangeRate, i.InvoiceDate })
            .ToListAsync(ct))
            .Select(x => new OpenItem(x.Party, x.Outstanding, asOf.DayNumber - x.InvoiceDate.DayNumber));

        var apItems = (await _apRepo.Query().AsNoTracking()
            .Where(i => (i.Status == SupplierInvoiceStatus.Approved || i.Status == SupplierInvoiceStatus.PartiallyPaid)
                     && i.InvoiceDate <= asOf && i.TotalAmount - i.AmountPaid > 0m)
            .Select(i => new { Party = i.Supplier.Name, Outstanding = (i.TotalAmount - i.AmountPaid) * i.ExchangeRate, i.InvoiceDate })
            .ToListAsync(ct))
            .Select(x => new OpenItem(x.Party, x.Outstanding, asOf.DayNumber - x.InvoiceDate.DayNumber));

        return ApiResponse<ArApAgingDto>.Ok(new ArApAgingDto(asOf, Build("Receivables", arItems), Build("Payables", apItems)));
    }
}

// ═══════════════════════════ I3 — P&L trend (last N months) ═══════════════════════════

public sealed record ProfitTrendPointDto(int Year, int Month, string Label, decimal Revenue, decimal Expense, decimal NetProfit);

public sealed record ProfitTrendDto(IReadOnlyList<ProfitTrendPointDto> Points);

public sealed record GetProfitTrendQuery(int Months = 12) : IRequest<ApiResponse<ProfitTrendDto>>;

internal sealed class GetProfitTrendQueryHandler : IRequestHandler<GetProfitTrendQuery, ApiResponse<ProfitTrendDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    public GetProfitTrendQueryHandler(IRepository<JournalEntryLine, long> lineRepo) => _lineRepo = lineRepo;

    private static readonly string[] MonthLabels =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    public async Task<ApiResponse<ProfitTrendDto>> Handle(GetProfitTrendQuery q, CancellationToken ct)
    {
        var n = Math.Clamp(q.Months, 1, 36);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-(n - 1));

        // Posted income/expense lines in the window (exclude year-end Closing).
        var raw = await _lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.VoucherType != VoucherType.Closing
                     && l.JournalEntry.EntryDate >= start
                     && (l.Account.AccountType == AccountType.Income || l.Account.AccountType == AccountType.Expense))
            .GroupBy(l => new { l.JournalEntry.EntryDate.Year, l.JournalEntry.EntryDate.Month, l.Account.AccountType })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.AccountType, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct);

        var points = new List<ProfitTrendPointDto>();
        for (var i = 0; i < n; i++)
        {
            var d = start.AddMonths(i);
            var rev = raw.Where(r => r.Year == d.Year && r.Month == d.Month && r.AccountType == AccountType.Income).Sum(r => r.Credit - r.Debit);
            var exp = raw.Where(r => r.Year == d.Year && r.Month == d.Month && r.AccountType == AccountType.Expense).Sum(r => r.Debit - r.Credit);
            points.Add(new ProfitTrendPointDto(d.Year, d.Month, MonthLabels[d.Month - 1],
                Math.Round(rev, 2), Math.Round(exp, 2), Math.Round(rev - exp, 2)));
        }

        return ApiResponse<ProfitTrendDto>.Ok(new ProfitTrendDto(points));
    }
}
