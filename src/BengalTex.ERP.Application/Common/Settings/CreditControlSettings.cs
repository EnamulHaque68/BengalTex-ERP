namespace BengalTex.ERP.Application.Common.Settings;

/// <summary>
/// Credit-control rules bound from the "CreditControl" section of appsettings.json.
/// Enforced at Sales Order confirmation: a customer's total credit exposure (outstanding
/// AR in BDT + the value of the order being confirmed) may not exceed their
/// <c>Customer.CreditLimit</c>. A CreditLimit of 0 means "no limit set" → not enforced
/// (so existing customers with no limit are unaffected).
/// </summary>
public class CreditControlSettings
{
    /// <summary>Master switch. When false, no credit check runs anywhere.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When true, confirming an over-limit order is blocked (raise the customer's credit
    /// limit, take a payment, or close older invoices first). When false, the check is
    /// advisory only (never blocks) — left for a future warning surface.
    /// </summary>
    public bool BlockOverLimit { get; set; } = true;
}
