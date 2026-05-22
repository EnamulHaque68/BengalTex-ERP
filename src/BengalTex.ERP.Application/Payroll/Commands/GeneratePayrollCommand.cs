using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Commands;

/// <summary>
/// Generates draft payslips for all active employees who don't yet have one for the
/// given month. Pay is computed from BasicSalary + that month's attendance:
///   perDay        = Basic / 30
///   Deductions    = AbsentDays × perDay
///   OvertimeAmount = OvertimeHours × (Basic / (30 × 8))   (default hourly rate; editable after)
///   Gross = Basic + Allowances(0) + OvertimeAmount ;  Net = Gross − Deductions
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
    private readonly IUnitOfWork _uow;

    public GeneratePayrollCommandHandler(
        IRepository<Payslip, long> payslipRepo,
        IRepository<Domain.Entities.Employee> employeeRepo,
        IRepository<AttendanceRecord, long> attendanceRepo,
        IUnitOfWork uow)
    {
        _payslipRepo = payslipRepo;
        _employeeRepo = employeeRepo;
        _attendanceRepo = attendanceRepo;
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

        var generated = 0;
        foreach (var emp in employees)
        {
            if (alreadyDone.Contains(emp.Id)) continue;

            byEmployee.TryGetValue(emp.Id, out var recs);
            recs ??= new();

            decimal presentDays = recs.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late)
                                  + 0.5m * recs.Count(r => r.Status == AttendanceStatus.HalfDay);
            decimal absentDays = recs.Count(r => r.Status == AttendanceStatus.Absent);
            decimal leaveDays = recs.Count(r => r.Status == AttendanceStatus.Leave);
            decimal otHours = recs.Sum(r => r.OvertimeHours);

            var basic = emp.BasicSalary;
            var perDay = basic / 30m;
            var deductions = Round(absentDays * perDay);
            var otAmount = Round(otHours * (basic / (30m * 8m)));
            decimal allowances = 0m;
            var gross = Round(basic + allowances + otAmount);
            var net = Round(gross - deductions);

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
                Deductions = deductions,
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
