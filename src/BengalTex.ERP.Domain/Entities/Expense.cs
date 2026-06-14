using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A spend category (Office Rent, Electricity, Transport …). Optionally mapped to a specific
/// expense <see cref="Account"/> in the chart of accounts — the account an approved expense
/// debits. When unmapped, the expense falls back to the default Administrative Expense account.
/// </summary>
public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Expense account this category posts to (Dr on approve). Null → default Admin Expense.</summary>
    public int? LedgerAccountId { get; set; }
    public Account? LedgerAccount { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// A recorded business expense. Draft is editable; Approving it records the spend and pays it
/// (auto-journal: Dr the category's expense account, Cr Cash/Bank by <see cref="PaymentMethod"/>).
/// Cancelling an approved expense posts a mirror reversal. Amounts are in base currency (BDT).
/// </summary>
public class Expense : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly ExpenseDate { get; set; }

    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>Who/what the money was paid to (free text).</summary>
    public string? Payee { get; set; }

    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }

    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
}

public enum ExpenseStatus
{
    Draft = 1,
    Approved = 2,           // recorded + paid (posted to the ledger)
    Cancelled = 3,
    PendingApproval = 4     // over the approval threshold — awaiting sign-off via the Approvals inbox
}
