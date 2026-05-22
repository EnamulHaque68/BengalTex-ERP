using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A Chart-of-Accounts node. Hierarchical (self-referencing <see cref="ParentAccountId"/>):
/// group accounts are headers (e.g. "Assets", "Current Assets") and are NOT postable; detail
/// accounts (e.g. "Cash in Hand", "Sales Revenue") receive journal postings. The natural
/// (normal) balance side is derived from <see cref="AccountType"/> — Asset/Expense are Debit,
/// Liability/Equity/Income are Credit.
/// </summary>
public class Account : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    /// <summary>Header account (no direct postings) vs detail/ledger account (postable).</summary>
    public bool IsGroup { get; set; }

    public int? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }
    public ICollection<Account> Children { get; set; } = new List<Account>();

    /// <summary>Seeded system account — protected from deletion / type change.</summary>
    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }
}

/// <summary>
/// The five fundamental account classes. Determines the normal balance side and which
/// financial statement the account rolls up into (Asset/Liability/Equity → Balance Sheet;
/// Income/Expense → Profit &amp; Loss).
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}
