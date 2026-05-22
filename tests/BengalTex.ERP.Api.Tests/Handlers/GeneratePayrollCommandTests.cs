using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

public class GeneratePayrollCommandTests
{
    private const int Year = 2026;
    private const int Month = 5;

    private static GeneratePayrollCommandHandler Handler(ApplicationDbContext ctx) =>
        new(new Repository<Payslip, long>(ctx),
            new Repository<Employee>(ctx),
            new Repository<AttendanceRecord, long>(ctx),
            new BengalTex.ERP.Infrastructure.Persistence.UnitOfWork(ctx));

    private static Employee ActiveEmployee(decimal basic) => new()
    {
        Code = "EMP-1", FullName = "Karim", BasicSalary = basic,
        IsActive = true, Status = EmployeeStatus.Active,
        EmploymentType = EmploymentType.Permanent, Gender = Gender.Male,
        JoiningDate = new DateOnly(2025, 1, 1)
    };

    private static AttendanceRecord Att(int empId, int day, AttendanceStatus status, decimal ot = 0m) => new()
    {
        EmployeeId = empId,
        AttendanceDate = new DateOnly(Year, Month, day),
        Status = status,
        OvertimeHours = ot
    };

    [Fact]
    public async Task Computes_pay_from_basic_salary_and_attendance()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee(30_000m);
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();

        ctx.AttendanceRecords.AddRange(
            Att(emp.Id, 1, AttendanceStatus.Present, ot: 10m),  // OT 10h
            Att(emp.Id, 2, AttendanceStatus.Absent),
            Att(emp.Id, 3, AttendanceStatus.Absent),
            Att(emp.Id, 4, AttendanceStatus.HalfDay));
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new GeneratePayrollCommand(Year, Month), default);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(1);

        var slip = await ctx.Payslips.SingleAsync();
        slip.PresentDays.Should().Be(1.5m);        // 1 Present + 0.5 × 1 HalfDay
        slip.AbsentDays.Should().Be(2m);
        slip.OvertimeHours.Should().Be(10m);
        slip.Deductions.Should().Be(2_000m);       // 2 × (30000/30)
        slip.OvertimeAmount.Should().Be(1_250m);   // 10 × (30000 / 240)
        slip.GrossPay.Should().Be(31_250m);        // basic + OT
        slip.NetPay.Should().Be(29_250m);          // gross − deductions
        slip.Status.Should().Be(PayslipStatus.Draft);
    }

    [Fact]
    public async Task Skips_employees_who_already_have_a_payslip_for_the_month()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee(30_000m);
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();
        ctx.Payslips.Add(new Payslip { EmployeeId = emp.Id, Year = Year, Month = Month, Status = PayslipStatus.Draft });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new GeneratePayrollCommand(Year, Month), default);

        result.Success.Should().BeFalse();   // nothing new to generate
        (await ctx.Payslips.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Fails_when_there_are_no_active_employees()
    {
        await using var ctx = TestHarness.NewContext();

        var result = await Handler(ctx).Handle(new GeneratePayrollCommand(Year, Month), default);

        result.Success.Should().BeFalse();
    }
}
