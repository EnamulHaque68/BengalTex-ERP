using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Commands;

/// <summary>
/// Phase A5 — payroll gross accrual. Approving a Draft payslip books the full earned salary as an
/// expense in the month it was earned and moves every deduction into its own payable, instead of
/// only expensing the net at pay time (the F3 fix). Posts (per payslip, dated month-end, tagged to
/// the employee's department cost center):
///   Dr 5200 Salary Expense       = NetPay + PfEmployee + IncomeTax + LoanDeduction  (earned gross)
///   Dr 5210 Employer PF          = PfEmployer
///     Cr 2130 Salary Payable     = NetPay
///     Cr 2135 PF Payable         = PfEmployee + PfEmployer
///     Cr 2160 AIT Payable        = IncomeTax
///     Cr 1190 Employee Loans     = LoanDeduction   (recovers the receivable)
/// Mark-Paid then only clears the net Salary Payable against Cash/Bank. Zero legs are dropped;
/// the entry balances by construction (earnedGross + employerPF = net + pf + ait + loan).
/// </summary>
public sealed record ApprovePayslipCommand(long Id) : IRequest<ApiResponse>;

public sealed class ApprovePayslipCommandValidator : AbstractValidator<ApprovePayslipCommand>
{
    public ApprovePayslipCommandValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

/// <summary>Bulk approve — accrues every still-Draft payslip for a payroll month in one atomic post.</summary>
public sealed record ApprovePayrollMonthCommand(int Year, int Month) : IRequest<ApiResponse<int>>;

public sealed class ApprovePayrollMonthCommandValidator : AbstractValidator<ApprovePayrollMonthCommand>
{
    public ApprovePayrollMonthCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

/// <summary>Shared payroll-accrual posting used by both the single and the bulk approve commands.</summary>
internal static class PayrollAccrual
{
    public static decimal EarnedGross(Payslip p) => p.NetPay + p.PfEmployee + p.IncomeTax + p.LoanDeduction;

    /// <summary>Builds the balanced accrual legs for one payslip (zero legs omitted).</summary>
    public static IReadOnlyList<JournalPostingLine> BuildLines(Payslip p, int? deptCc)
    {
        var dims = deptCc is null ? (Dimensions?)null : new Dimensions(CostCenterId: deptCc);
        var lines = new List<JournalPostingLine>();

        var earnedGross = EarnedGross(p);
        if (earnedGross > 0m) lines.Add(new(LedgerAccounts.SalaryExpense, earnedGross, 0m, dims));
        if (p.PfEmployer > 0m) lines.Add(new(LedgerAccounts.EmployerPfGratuity, p.PfEmployer, 0m, dims));

        if (p.NetPay > 0m) lines.Add(new(LedgerAccounts.SalaryPayable, 0m, p.NetPay));
        var pfPayable = p.PfEmployee + p.PfEmployer;
        if (pfPayable > 0m) lines.Add(new(LedgerAccounts.ProvidentFundPayable, 0m, pfPayable));
        if (p.IncomeTax > 0m) lines.Add(new(LedgerAccounts.AitPayable, 0m, p.IncomeTax));
        if (p.LoanDeduction > 0m) lines.Add(new(LedgerAccounts.EmployeeLoans, 0m, p.LoanDeduction));

        return lines;
    }

    /// <summary>Month-end date so the accrual lands in the month the salary was earned.</summary>
    public static DateOnly MonthEnd(int year, int month) => new(year, month, DateTime.DaysInMonth(year, month));

    /// <summary>Department cost center for an employee (null when the department has no active CC).</summary>
    public static async Task<int?> DeptCostCenterAsync(
        IRepository<Domain.Entities.CostCenter> ccRepo, int? departmentId, CancellationToken ct)
    {
        if (departmentId is not int deptId) return null;
        return await ccRepo.Query().AsNoTracking()
            .Where(c => c.DepartmentId == deptId && c.IsActive)
            .Select(c => (int?)c.Id).FirstOrDefaultAsync(ct);
    }
}

internal sealed class ApprovePayslipCommandHandler : IRequestHandler<ApprovePayslipCommand, ApiResponse>
{
    private readonly IRepository<Payslip, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IRepository<Domain.Entities.CostCenter> _ccRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;

    public ApprovePayslipCommandHandler(
        IRepository<Payslip, long> repo,
        IRepository<Domain.Entities.Employee> empRepo,
        IRepository<Domain.Entities.CostCenter> ccRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IJournalPostingService journal)
    {
        _repo = repo; _empRepo = empRepo; _ccRepo = ccRepo; _uow = uow;
        _currentUser = currentUser; _journal = journal;
    }

    public async Task<ApiResponse> Handle(ApprovePayslipCommand cmd, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(cmd.Id, ct);
        if (p is null) return ApiResponse.Fail("Payslip not found.");
        if (p.Status != PayslipStatus.Draft)
            return ApiResponse.Fail($"Only a Draft payslip can be approved (this one is {p.Status}).");

        var emp = await _empRepo.Query().AsNoTracking().Where(e => e.Id == p.EmployeeId)
            .Select(e => new { e.Code, e.FullName, e.DepartmentId }).FirstOrDefaultAsync(ct);
        var deptCc = await PayrollAccrual.DeptCostCenterAsync(_ccRepo, emp?.DepartmentId, ct);

        var lines = PayrollAccrual.BuildLines(p, deptCc);
        if (lines.Count > 0)
        {
            var empLabel = emp is null ? $"Emp #{p.EmployeeId}" : $"{emp.FullName} ({emp.Code})";
            await _journal.PostAsync(
                PayrollAccrual.MonthEnd(p.Year, p.Month),
                $"Salary accrual {p.Year}-{p.Month:D2} — {empLabel}",
                "PayslipAccrual", p.Id, $"PS-{p.Year}{p.Month:D2}-{p.EmployeeId}",
                lines, ct);
        }

        p.Status = PayslipStatus.Approved;
        p.ApprovedAt = DateTimeOffset.UtcNow;
        p.ApprovedBy = _currentUser.UserName;
        _repo.Update(p);

        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Payslip approved — salary accrued.");
    }
}

internal sealed class ApprovePayrollMonthCommandHandler : IRequestHandler<ApprovePayrollMonthCommand, ApiResponse<int>>
{
    private readonly IRepository<Payslip, long> _repo;
    private readonly IRepository<Domain.Entities.CostCenter> _ccRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;

    public ApprovePayrollMonthCommandHandler(
        IRepository<Payslip, long> repo,
        IRepository<Domain.Entities.CostCenter> ccRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IJournalPostingService journal)
    {
        _repo = repo; _ccRepo = ccRepo; _uow = uow; _currentUser = currentUser; _journal = journal;
    }

    public async Task<ApiResponse<int>> Handle(ApprovePayrollMonthCommand cmd, CancellationToken ct)
    {
        var drafts = await _repo.Query()
            .Where(p => p.Year == cmd.Year && p.Month == cmd.Month && p.Status == PayslipStatus.Draft)
            .Select(p => new
            {
                Slip = p,
                p.Employee.Code, p.Employee.FullName, p.Employee.DepartmentId
            })
            .ToListAsync(ct);
        if (drafts.Count == 0) return ApiResponse<int>.Fail("No Draft payslips to approve for this month.");

        // Resolve each department's cost center once.
        var deptIds = drafts.Where(d => d.DepartmentId.HasValue).Select(d => d.DepartmentId!.Value).Distinct().ToList();
        var ccByDept = await _ccRepo.Query().AsNoTracking()
            .Where(c => c.DepartmentId != null && deptIds.Contains(c.DepartmentId.Value) && c.IsActive)
            .GroupBy(c => c.DepartmentId!.Value)
            .Select(g => new { DeptId = g.Key, CcId = g.Min(c => c.Id) })
            .ToDictionaryAsync(x => x.DeptId, x => (int?)x.CcId, ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var d in drafts)
        {
            int? deptCc = d.DepartmentId.HasValue && ccByDept.TryGetValue(d.DepartmentId.Value, out var cc) ? cc : null;
            var lines = PayrollAccrual.BuildLines(d.Slip, deptCc);
            if (lines.Count > 0)
                await _journal.PostAsync(
                    PayrollAccrual.MonthEnd(d.Slip.Year, d.Slip.Month),
                    $"Salary accrual {d.Slip.Year}-{d.Slip.Month:D2} — {d.FullName} ({d.Code})",
                    "PayslipAccrual", d.Slip.Id, $"PS-{d.Slip.Year}{d.Slip.Month:D2}-{d.Slip.EmployeeId}",
                    lines, ct);

            d.Slip.Status = PayslipStatus.Approved;
            d.Slip.ApprovedAt = now;
            d.Slip.ApprovedBy = _currentUser.UserName;
            _repo.Update(d.Slip);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(drafts.Count, $"{drafts.Count} payslip(s) approved and accrued.");
    }
}
