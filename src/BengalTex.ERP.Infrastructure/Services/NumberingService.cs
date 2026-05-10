using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

public class NumberingService : INumberingService
{
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;

    public NumberingService(ApplicationDbContext db, IDateTimeProvider dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<string> NextAsync(string seriesCode, int? factoryId = null, CancellationToken ct = default)
    {
        var now = _dateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        // Use a serializable lock via UPDLOCK to prevent concurrent number issuance.
        // EF Core 8: use FromSqlInterpolated for hint; or just rely on a transaction.
        await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var series = await _db.NumberingSeries
            .Where(s => s.Code == seriesCode && s.FactoryId == factoryId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"Numbering series '{seriesCode}' (factory={factoryId}) not configured.");

        // Reset logic
        var shouldReset = series.ResetCycle switch
        {
            ResetCycle.Yearly => series.CurrentYear != year,
            ResetCycle.Monthly => series.CurrentYear != year || series.CurrentMonth != month,
            _ => false
        };

        if (shouldReset)
        {
            series.CurrentNumber = 0;
            series.CurrentYear = year;
            series.CurrentMonth = month;
        }

        series.CurrentNumber += 1;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var parts = new List<string> { series.Prefix };
        if (series.IncludeYear) parts.Add(year.ToString("D4"));
        if (series.IncludeMonth) parts.Add(month.ToString("D2"));
        parts.Add(series.CurrentNumber.ToString().PadLeft(series.PaddingLength, '0'));
        return string.Join(series.Separator, parts);
    }
}