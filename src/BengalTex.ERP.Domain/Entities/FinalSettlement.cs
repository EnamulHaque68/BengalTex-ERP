using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Full-and-final settlement when an employee leaves. Computes prorated final-month
/// salary + unused leave encashment + gratuity (BD Labour Act 2006: 30 days basic per
/// completed year for 5-9y service, 60 days for 10y+) − outstanding loans − other
/// deductions = net payable. On Approve, the employee is auto-marked Inactive and any
/// Active EmployeeLoans for them are auto-Closed (written off against this settlement's
/// deduction). On Cancel from Approved, the employee/loan changes are reverted.
/// </summary>
public class FinalSettlement : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;          // "FS-####"

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly SettlementDate { get; set; }              // when net is paid
    public DateOnly LastWorkingDate { get; set; }             // employee's last working day
    public DateOnly JoiningDate { get; set; }                 // snapshot from employee
    public decimal YearsOfService { get; set; }               // computed (≥6mo partial year counts)

    public SettlementReason Reason { get; set; } = SettlementReason.Resignation;

    // ── Earnings (BDT) ──
    public decimal BasicSalary { get; set; }                  // snapshot from employee at settlement time
    public decimal ProratedDays { get; set; }                 // days worked in final month
    public decimal ProratedSalary { get; set; }               // basic × proratedDays / 30
    public decimal LeaveEncashmentDays { get; set; }          // unused paid-leave balance summed
    public decimal LeaveEncashmentAmount { get; set; }        // days × (basic / 30)
    public decimal GratuityAmount { get; set; }               // BD Labour Act formula
    public decimal OtherEarnings { get; set; }                // manual: ad-hoc bonus, dues etc.

    // ── Deductions (BDT) ──
    public decimal OutstandingLoan { get; set; }              // snapshot of sum(active loan OutstandingPrincipal)
    public decimal OtherDeductions { get; set; }              // manual: advances, asset write-off, etc.

    // ── Totals ──
    public decimal GrossPayable { get; set; }                 // sum of earnings
    public decimal TotalDeductions { get; set; }              // sum of deductions
    public decimal NetPayable { get; set; }                   // GrossPayable − TotalDeductions

    public FinalSettlementStatus Status { get; set; } = FinalSettlementStatus.Draft;

    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedByUser { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }                // "Cash" / "BankTransfer" / "Cheque"
    public string? PaymentReference { get; set; }

    public string? Notes { get; set; }
}

public enum FinalSettlementStatus
{
    Draft = 1,
    Approved = 2,
    Paid = 3,
    Cancelled = 4
}

public enum SettlementReason
{
    Resignation = 1,
    Termination = 2,
    Retirement = 3,
    Death = 4,
    EndOfContract = 5
}
