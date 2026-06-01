using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Company bank-account master. Used by Payment/Receipt/Payroll disbursement records to
/// identify WHICH bank the money moved through. The optional <c>LedgerAccountId</c> links
/// this bank account to a specific Chart-of-Accounts Bank node — enables future Bank
/// Reconciliation to match statement lines to journal lines for this account.
/// </summary>
public class BankAccount : BaseEntity
{
    /// <summary>Friendly name shown in pickers (e.g. "Bengal Tex Operating - Sonali Bank").</summary>
    public string AccountName { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }

    public string AccountNumber { get; set; } = string.Empty;
    public BankAccountType AccountType { get; set; } = BankAccountType.Current;

    public string? RoutingNumber { get; set; }
    public string? SwiftCode { get; set; }

    public string Currency { get; set; } = "BDT";

    /// <summary>FK to Chart of Accounts (typically a Bank-type account under 1120). Optional.</summary>
    public int? LedgerAccountId { get; set; }
    public Account? LedgerAccount { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum BankAccountType
{
    Current = 1,
    Savings = 2,
    STD = 3,             // Short Term Deposit
    FixedDeposit = 4,
    Other = 99
}
