using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A bank statement loaded for reconciliation against the ledger. v1a is manual entry;
/// v1b will add CSV/Excel import. Each statement has many <see cref="BankStatementLine"/>s
/// that get individually matched to <see cref="JournalEntryLine"/>s posted on the linked
/// <see cref="BankAccount.LedgerAccountId"/>. Reconciliation completes when all lines are
/// Matched or Excluded AND opening + Σ signed line amounts = closing balance.
/// </summary>
public class BankStatement : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;     // "BST-####"

    public int BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public DateOnly StatementDate { get; set; }
    public DateOnly PeriodFromDate { get; set; }
    public DateOnly PeriodToDate { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }

    public bool IsReconciled { get; set; }
    public DateTimeOffset? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}

/// <summary>
/// One transaction row on a <see cref="BankStatement"/>. Amount is SIGNED:
/// positive = inflow (deposit into bank), negative = outflow (withdrawal from bank).
/// Maps to a JournalEntryLine when matched.
/// </summary>
public class BankStatementLine : BaseTransactionalEntity
{
    public long BankStatementId { get; set; }
    public BankStatement BankStatement { get; set; } = null!;

    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }

    /// <summary>Signed amount: + deposit, − withdrawal.</summary>
    public decimal Amount { get; set; }

    public BankStatementLineStatus Status { get; set; } = BankStatementLineStatus.Unmatched;

    /// <summary>When Matched, points at the journal line on the bank's ledger account.</summary>
    public long? MatchedJournalLineId { get; set; }
    public JournalEntryLine? MatchedJournalLine { get; set; }

    public DateTimeOffset? MatchedAt { get; set; }
    public string? MatchedBy { get; set; }

    public string? Notes { get; set; }
}

public enum BankStatementLineStatus
{
    Unmatched = 1,
    Matched = 2,
    /// <summary>Bank-side adjustments (fees, interest) that don't have a ledger counterpart yet — excluded from rec.</summary>
    Excluded = 3
}
