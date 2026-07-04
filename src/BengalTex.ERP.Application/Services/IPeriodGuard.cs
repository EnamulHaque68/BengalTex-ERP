namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Fiscal-period posting guard (Phase A1). One check point used by BOTH the auto-journal
/// engine (<see cref="IJournalPostingService"/>) and the manual-voucher commands, so a single
/// rule protects every posting path.
///
/// Rules: date not covered by any fiscal year → allowed (backward compatible — the guard only
/// activates once fiscal years are defined); period Open → allowed; SoftClosed → auto-journals
/// allowed, manual vouchers require the <c>Accounting.CloseBooks</c> permission; Locked →
/// nothing posts.
/// </summary>
public interface IPeriodGuard
{
    /// <summary>Null when posting to <paramref name="date"/> is allowed; otherwise the human-readable refusal.</summary>
    Task<string?> CheckAsync(DateOnly date, bool isManualVoucher, CancellationToken ct = default);

    /// <summary>Id of the accounting period covering the date, or null when no fiscal year covers it.</summary>
    Task<int?> GetPeriodIdAsync(DateOnly date, CancellationToken ct = default);
}
