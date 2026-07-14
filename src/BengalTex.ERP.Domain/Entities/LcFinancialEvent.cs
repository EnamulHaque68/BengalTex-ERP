using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Phase A6a — a financial event on a <see cref="LetterOfCredit"/>. Each event posts its own
/// journal, turning the LC into a real sub-ledger of bank finance (margin locked, charges,
/// document retirement into PAD / acceptance, interest, and settlement). The event's journal is
/// tagged SourceType "LcFinancialEvent" + SourceId = this row's Id.
/// </summary>
public class LcFinancialEvent : BaseTransactionalEntity
{
    public long LetterOfCreditId { get; set; }
    public LetterOfCredit LetterOfCredit { get; set; } = null!;

    public LcEventType EventType { get; set; }

    public DateOnly EventDate { get; set; }

    /// <summary>Primary amount of the event, in base BDT.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// For a document-retirement / acceptance event — the portion of the LC margin (1185) applied
    /// toward the payment. The bank finances the remainder (Amount − MarginApplied) as PAD/Acceptance.
    /// </summary>
    public decimal MarginApplied { get; set; }

    /// <summary>Cash|Bank the money moved through (for margin / charge / interest / settlement events).</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    /// <summary>Bank advice / challan / document reference.</summary>
    public string? Reference { get; set; }

    public string? Notes { get; set; }
}

public enum LcEventType
{
    MarginDeposit = 1,          // Dr 1185 LC Margin / Cr Cash|Bank
    BankCharge = 2,             // Dr 5600 Bank Charges / Cr Cash|Bank
    RetirementSight = 3,        // Dr 2110 AP / Cr 1185 Margin (applied) + Cr 2180 PAD (financed)  — sight LC
    AcceptanceUsance = 4,       // Dr 2110 AP / Cr 1185 Margin (applied) + Cr 2190 Acceptance      — usance/UPAS
    Interest = 5,               // Dr 5860 Interest Expense / Cr Cash|Bank  — cost of credit
    PadSettlement = 6,          // Dr 2180 PAD / Cr Cash|Bank
    AcceptanceSettlement = 7    // Dr 2190 Acceptance / Cr Cash|Bank
}
