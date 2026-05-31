using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// One-off bonus paid outside the monthly payslip (Eid-ul-Fitr, Eid-ul-Azha, Pohela Boishakh).
/// One row per (Employee, Year, BonusType) is enforced unique. PayFestivalBonus auto-journals
/// Dr Salary Expense / Cr Cash|Bank for Amount (same pattern as MarkPayslipPaid).
/// </summary>
public class FestivalBonus : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;       // "FB-####"

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int BonusYear { get; set; }
    public FestivalBonusType BonusType { get; set; }

    public decimal Amount { get; set; }

    public FestivalBonusStatus Status { get; set; } = FestivalBonusStatus.Draft;

    /// <summary>Cash / BankTransfer / Cheque / MobileBanking / Other (reuses <see cref="PaymentMethod"/> enum).</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    public DateTimeOffset? PaidAt { get; set; }
    public string? PaidBy { get; set; }

    public string? Notes { get; set; }
}

public enum FestivalBonusType
{
    EidUlFitr = 1,
    EidUlAzha = 2,
    PohelaBoishakh = 3,
    Other = 99
}

public enum FestivalBonusStatus
{
    Draft = 1,
    Paid = 2,
    Cancelled = 3
}
