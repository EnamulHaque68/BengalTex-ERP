using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Commands;

/// <summary>
/// Generates draft payslips for all active employees who don't yet have one for the
/// given month. Pay = Basic + Allowances(legacy) + BD components (HouseRent/Medical/Transport/Food)
/// + OvertimeAmount; Deductions = absence-based + PF (if opted-in) + IncomeTax (if opted-in)
/// + LoanDeduction (auto-summed from this employee's Active <see cref="EmployeeLoan"/>s whose
///   StartYearMonth ≤ current YYYYMM; per-loan deduction = min(EmiAmount, OutstandingPrincipal);
///   loan auto-Closes when OutstandingPrincipal hits 0).
/// </summary>
public sealed record GeneratePayrollCommand(int Year, int Month) : IRequest<ApiResponse<int>>;

public sealed class GeneratePayrollCommandValidator : AbstractValidator<GeneratePayrollCommand>
{
    public GeneratePayrollCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

internal sealed class GeneratePayrollCommandHandler
    : IRequestHandler<GeneratePayrollCommand, ApiResponse<int>>
{
    private readonly IRepository<Payslip, long> _payslipRepo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<AttendanceRecord, long> _attendanceRepo;
    private readonly IRepository<EmployeeLoan, long> _loanRepo;
    private readonly IUnitOfWork _uow;

    public GeneratePayrollCommandHandler(
        IRepository<Payslip, long> payslipRepo,
        IRepository<Domain.Entities.Employee> employeeRepo,
        IRepository<AttendanceRecord, long> attendanceRepo,
        IRepository<EmployeeLoan, long> loanRepo,
        IUnitOfWork uow)
    {
        _payslipRepo = payslipRepo;
        _employeeRepo = employeeRepo;
        _attendanceRepo = attendanceRepo;
        _loanRepo = loanRepo;
        _uow = uow;
    }

    public async Task<ApiResponse<int>> Handle(GeneratePayrollCommand cmd, CancellationToken ct)
    {
        var monthStart = new DateOnly(cmd.Year, cmd.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var employees = await _employeeRepo.Query()
            .Where(e => e.IsActive && e.Status == EmployeeStatus.Active)
            .ToListAsync(ct);
        if (employees.Count == 0) return ApiResponse<int>.Fail("No active employees to generate payroll for.");

        var alreadyDone = (await _payslipRepo.Query()
                .Where(p => p.Year == cmd.Year && p.Month == cmd.Month)
                .Select(p => p.EmployeeId)
                .ToListAsync(ct))
            .ToHashSet();

        var attendance = await _attendanceRepo.Query()
            .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= monthEnd)
            .Select(a => new { a.EmployeeId, a.Status, a.OvertimeHours })
            .ToListAsync(ct);
        var byEmployee = attendance.GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        // Active loans whose start month is at or before the payroll month
        var currentYm = cmd.Year * 100 + cmd.Month;
        var activeLoans = await _loanRepo.Query()
            .Where(l => l.Status == EmployeeLoanStatus.Active
                        && l.OutstandingPrincipal > 0m
                        && l.StartYearMonth <= currentYm)
            .ToListAsync(ct);
        var loansByEmployee = activeLoans.GroupBy(l => l.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var generated = 0;
        foreach (var emp in employees)
        {
            if (alreadyDone.Contains(emp.Id)) continue;

            byEmployee.TryGetValue(emp.Id, out var recs);
            recs ??= new();

            decimal presentDays = recs.Count(r => r.Status.CountsAsFullPresent())
                                  + 0.5m * recs.Count(r => r.Status == AttendanceStatus.HalfDay);
            decimal absentDays = recs.Count(r => r.Status == AttendanceStatus.Absent);
            decimal leaveDays = recs.Count(r => r.Status == AttendanceStatus.Leave);
            decimal otHours = recs.Sum(r => r.OvertimeHours);

            var basic = emp.BasicSalary;
            var perDay = basic / 30m;
            var absenceDeduction = Round(absentDays * perDay);
            var otAmount = Round(otHours * (basic / (30m * 8m)));
            decimal allowances = 0m;

            // BD breakdown components (snapshot from Employee)
            var houseRent = emp.HouseRentAllowance;
            var medical = emp.MedicalAllowance;
            var transport = emp.TransportAllowance;
            var food = emp.FoodAllowance;
            decimal festivalBonus = 0m;  // managed via separate workflow in v1b

            var gross = Round(basic + allowances + otAmount + houseRent + medical + transport + food + festivalBonus);

            var pfEmployee = emp.IsPfMember ? Round(basic * emp.PfRate / 100m) : 0m;
            var pfEmployer = pfEmployee;
            var incomeTax = emp.IsTaxable ? BangladeshIncomeTax.MonthlyTaxOf(gross) : 0m;

            // Auto-deduct EMI from each active loan; loan auto-closes when fully repaid.
            decimal loanDeduction = 0m;
            if (loansByEmployee.TryGetValue(emp.Id, out var loans))
            {
                foreach (var loan in loans)
                {
                    var thisMonth = Math.Min(loan.EmiAmount, loan.OutstandingPrincipal);
                    if (thisMonth <= 0m) continue;
                    loanDeduction += thisMonth;
                    loan.OutstandingPrincipal -= thisMonth;
                    if (loan.OutstandingPrincipal <= 0m)
                    {
                        loan.OutstandingPrincipal = 0m;
                        loan.Status = EmployeeLoanStatus.Closed;
                    }
                    _loanRepo.Update(loan);
                }
                loanDeduction = Round(loanDeduction);
            }

            var totalDeductions = Round(absenceDeduction + pfEmployee + incomeTax + loanDeduction);
            var net = Round(gross - totalDeductions);

            await _payslipRepo.AddAsync(new Payslip
            {
                EmployeeId = emp.Id,
                Year = cmd.Year,
                Month = cmd.Month,
                BasicSalary = basic,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LeaveDays = leaveDays,
                OvertimeHours = otHours,
                OvertimeAmount = otAmount,
                Allowances = allowances,
                Deductions = totalDeductions,
                HouseRent = houseRent,
                Medical = medical,
                Transport = transport,
                FoodAllowance = food,
                FestivalBonus = festivalBonus,
                PfEmployee = pfEmployee,
                PfEmployer = pfEmployer,
                IncomeTax = incomeTax,
                LoanDeduction = loanDeduction,
                GrossPay = gross,
                NetPay = net,
                Status = PayslipStatus.Draft
            }, ct);
            generated++;
        }

        if (generated == 0)
            return ApiResponse<int>.Fail("All active employees already have a payslip for this month.");

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(generated, $"{generated} payslip(s) generated.");
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
