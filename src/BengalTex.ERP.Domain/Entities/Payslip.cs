using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Monthly payslip for an <see cref="Employee"/> — one per (employee, year, month), unique.
/// Generated from the employee's BasicSalary + that month's <see cref="AttendanceRecord"/>s,
/// then adjustable (allowances / overtime / deductions) before being marked Paid.
///   GrossPay = BasicSalary + Allowances + OvertimeAmount
///   NetPay   = GrossPay − Deductions
/// </summary>
public class Payslip : BaseTransactionalEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }     // 1-12

    public decimal BasicSalary { get; set; }   // snapshot at generation time

    // Attendance-derived (decimals so half-days work)
    public decimal PresentDays { get; set; }
    public decimal AbsentDays { get; set; }
    public decimal LeaveDays { get; set; }
    public decimal OvertimeHours { get; set; }

    public decimal OvertimeAmount { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }

    // BD payroll breakdown — additive earnings (snapshotted from Employee at generation)
    public decimal HouseRent { get; set; }
    public decimal Medical { get; set; }
    public decimal Transport { get; set; }
    public decimal FoodAllowance { get; set; }
    public decimal FestivalBonus { get; set; }

    // BD payroll breakdown — additive deductions
    public decimal PfEmployee { get; set; }
    public decimal PfEmployer { get; set; }   // not deducted from net; for ledger/reporting
    public decimal IncomeTax { get; set; }
    public decimal LoanDeduction { get; set; }

    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }

    public PayslipStatus Status { get; set; } = PayslipStatus.Draft;

    // Phase A5 — gross accrual: on Approve the full earned-gross expense + deduction payables are
    // posted (Salary/PF Expense Dr; Salary/PF/AIT Payable + Loan Cr); Mark-Paid then only clears
    // the net Salary Payable against Cash/Bank.
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public DateTimeOffset? PaidAt { get; set; }
    public string? PaidBy { get; set; }

    public string? Notes { get; set; }
}

public enum PayslipStatus
{
    Draft = 1,
    Paid = 2,
    Approved = 3   // accrued (gross expensed, deductions moved to payables) — awaiting net payment
}
