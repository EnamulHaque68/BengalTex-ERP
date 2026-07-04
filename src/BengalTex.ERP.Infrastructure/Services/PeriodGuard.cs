using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Shared.Permissions;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IPeriodGuard"/> over the AccountingPeriods table. See the interface
/// for the rule matrix. SuperAdmin is NOT exempt from Locked periods — reopening the period
/// (audited) is the sanctioned path.
/// </summary>
public sealed class PeriodGuard : IPeriodGuard
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PeriodGuard(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<string?> CheckAsync(DateOnly date, bool isManualVoucher, CancellationToken ct = default)
    {
        var period = await FindAsync(date, ct);
        if (period is null) return null;   // no fiscal year covers the date → guard inactive

        return period.Status switch
        {
            AccountingPeriodStatus.Open => null,
            AccountingPeriodStatus.Locked =>
                $"Accounting period {period.Name} is locked — no postings allowed. " +
                "A user with the Close-Books permission must reopen it first.",
            AccountingPeriodStatus.SoftClosed when isManualVoucher
                                                && !_currentUser.HasPermission(Permissions.Accounting.CloseBooks) =>
                $"Accounting period {period.Name} is soft-closed — manual vouchers require the Close-Books permission.",
            _ => null   // SoftClosed + auto-journal, or SoftClosed + CloseBooks holder
        };
    }

    public async Task<int?> GetPeriodIdAsync(DateOnly date, CancellationToken ct = default)
        => (await FindAsync(date, ct))?.Id;

    private Task<AccountingPeriod?> FindAsync(DateOnly date, CancellationToken ct) =>
        _db.AccountingPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.StartDate <= date && p.EndDate >= date, ct);
}
