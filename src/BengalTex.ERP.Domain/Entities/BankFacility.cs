using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Phase A6c — a bank treasury facility: a term loan, an overdraft / cash-credit line, or a fixed
/// deposit (FDR). Financial events (drawdown, interest, repayment / placement, income, encashment)
/// are captured as <see cref="BankFacilityEvent"/> rows, each posting its own journal — the facility
/// becomes a sub-ledger of borrowing (2210) or FDR investment (1250).
/// </summary>
public class BankFacility : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;        // "BF-####"

    public BankFacilityType FacilityType { get; set; }

    public string BankName { get; set; } = string.Empty;
    public string? AccountReference { get; set; }           // facility / loan / FDR account no.

    /// <summary>Sanctioned principal / OD limit / FDR deposit, in BDT.</summary>
    public decimal Amount { get; set; }

    /// <summary>Annual interest rate in percent (informational).</summary>
    public decimal InterestRate { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? MaturityDate { get; set; }

    public BankFacilityStatus Status { get; set; } = BankFacilityStatus.Active;

    public string? Notes { get; set; }

    public ICollection<BankFacilityEvent> Events { get; set; } = new List<BankFacilityEvent>();
}

/// <summary>Phase A6c — a financial event on a <see cref="BankFacility"/>; each posts its own journal.</summary>
public class BankFacilityEvent : BaseTransactionalEntity
{
    public long BankFacilityId { get; set; }
    public BankFacility BankFacility { get; set; } = null!;

    public BankFacilityEventType EventType { get; set; }

    public DateOnly EventDate { get; set; }

    /// <summary>Event amount, in BDT.</summary>
    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public enum BankFacilityType
{
    TermLoan = 1,       // Cr 2210 on drawdown
    OverdraftCC = 2,    // Cr 2210 on drawdown (revolving)
    Fdr = 3             // Dr 1250 on placement
}

public enum BankFacilityStatus
{
    Active = 1,
    Closed = 2
}

public enum BankFacilityEventType
{
    Drawdown = 1,           // Dr Bank / Cr 2210 Bank Loan            (loan / OD)
    InterestCharge = 2,     // Dr 5860 Interest Expense / Cr Bank     (loan / OD)
    PrincipalRepayment = 3, // Dr 2210 / Cr Bank                      (loan / OD)
    FdrPlacement = 4,       // Dr 1250 FDR / Cr Bank                  (FDR)
    FdrInterestIncome = 5,  // Dr Bank / Cr 4200 Other Income         (FDR)
    FdrEncashment = 6       // Dr Bank / Cr 1250 FDR                  (FDR)
}
