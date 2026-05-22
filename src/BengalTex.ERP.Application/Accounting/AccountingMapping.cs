using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Accounting;

/// <summary>Shared accounting helpers — single source of the normal-balance rule.</summary>
public static class AccountingMapping
{
    /// <summary>Asset &amp; Expense accounts are debit-normal; Liability/Equity/Income are credit-normal.</summary>
    public static string NormalBalanceOf(AccountType type) =>
        type is AccountType.Asset or AccountType.Expense ? "Debit" : "Credit";

    public static bool IsDebitNormal(AccountType type) =>
        type is AccountType.Asset or AccountType.Expense;
}
