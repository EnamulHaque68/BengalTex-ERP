using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Employee loan + EMI tracker. On GeneratePayroll, for each Active loan the EMI is
/// auto-deducted into <see cref="Payslip.LoanDeduction"/> and <see cref="OutstandingPrincipal"/>
/// reduces accordingly; loan auto-Closes when fully repaid. Interest-free in v1b (flat EMI =
/// Principal / TenureMonths); v1c will add InterestRate + flat-vs-reducing schedule.
/// </summary>
public class EmployeeLoan : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;     // "LN-####"

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly IssuedDate { get; set; }

    /// <summary>Loan principal in BDT.</summary>
    public decimal Principal { get; set; }

    /// <summary>Per-month deduction amount in BDT.</summary>
    public decimal EmiAmount { get; set; }

    public int TenureMonths { get; set; }

    /// <summary>YYYYMM (e.g. 202607) — first month EMI is deducted. Defaults to IssuedDate month.</summary>
    public int StartYearMonth { get; set; }

    /// <summary>Denormalised: Principal − Σ(deducted in posted Payslips). Auto-Closes at 0.</summary>
    public decimal OutstandingPrincipal { get; set; }

    public EmployeeLoanStatus Status { get; set; } = EmployeeLoanStatus.Active;

    /// <summary>Phase A5 — true once the disbursement journal (Dr Employee Loans / Cr Cash|Bank) is posted.</summary>
    public bool IsGlPosted { get; set; }

    public string? Notes { get; set; }
}

public enum EmployeeLoanStatus
{
    Active = 1,
    Closed = 2,
    Cancelled = 3
}
