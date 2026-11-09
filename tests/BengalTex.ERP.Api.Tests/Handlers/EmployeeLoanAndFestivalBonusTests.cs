using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

public class EmployeeLoanAndFestivalBonusTests
{
    private readonly Mock<IJournalPostingService> _journal = new();
    private readonly List<(string Account, decimal Debit, decimal Credit)> _journalLines = new();

    public EmployeeLoanAndFestivalBonusTests()
    {
        _journal.Setup(j => j.PostAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<JournalPostingLine>>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, string, string, long, string, IReadOnlyList<JournalPostingLine>, CancellationToken>(
                (_, _, _, _, _, lines, _) =>
                {
                    foreach (var l in lines)
                    {
                        _journalLines.Add((l.AccountCode, l.Debit, l.Credit));
                    }
                })
            .Returns(Task.CompletedTask);
    }

    private static Employee ActiveEmployee(string code = "EMP-001", decimal basic = 30_000m) => new()
    {
        Code = code,
        FullName = "Rahim Accessories Master",
        BasicSalary = basic,
        IsActive = true,
        Status = EmployeeStatus.Active,
        EmploymentType = EmploymentType.Permanent,
        Gender = Gender.Male,
        JoiningDate = new DateOnly(2025, 1, 1)
    };

    // =========================================================================
    // 1. EMPLOYEE LOAN VALIDATION TESTS
    // =========================================================================

    [Theory]
    [InlineData(0, 10000, 1000, 10, 202605)]       // Invalid EmployeeId
    [InlineData(1, 0, 1000, 10, 202605)]           // Zero Principal
    [InlineData(1, -5000, 1000, 10, 202605)]       // Negative Principal
    [InlineData(1, 10000, 0, 10, 202605)]          // Zero EMI
    [InlineData(1, 10000, 15000, 10, 202605)]      // EMI > Principal
    [InlineData(1, 10000, 1000, 0, 202605)]        // Zero Tenure
    [InlineData(1, 10000, 1000, 121, 202605)]      // Tenure > 120
    [InlineData(1, 10000, 1000, 10, 202600)]       // Invalid month 00
    [InlineData(1, 10000, 1000, 10, 202613)]       // Invalid month 13
    public void CreateEmployeeLoanValidator_Fails_For_Invalid_Inputs(
        int empId, decimal principal, decimal emi, int tenure, int startYm)
    {
        var validator = new CreateEmployeeLoanCommandValidator();
        var cmd = new CreateEmployeeLoanCommand(empId, new DateOnly(2026, 5, 1), principal, emi, tenure, startYm, "Notes");
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateEmployeeLoanValidator_Passes_For_Valid_Input()
    {
        var validator = new CreateEmployeeLoanCommandValidator();
        var cmd = new CreateEmployeeLoanCommand(1, new DateOnly(2026, 5, 1), 20000m, 2000m, 10, 202605, "Valid loan");
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    // =========================================================================
    // 2. EMPLOYEE LOAN LIFECYCLE & GL POSTING TESTS
    // =========================================================================

    [Fact]
    public async Task CreateEmployeeLoan_Rejects_Inactive_Or_Nonexistent_Employee()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee();
        emp.IsActive = false;
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();

        var handler = new CreateEmployeeLoanCommandHandler(
            new Repository<EmployeeLoan, long>(ctx),
            new Repository<Employee>(ctx),
            new UnitOfWork(ctx),
            TestHarness.Numbering().Object,
            _journal.Object);

        var result = await handler.Handle(
            new CreateEmployeeLoanCommand(emp.Id, new DateOnly(2026, 5, 1), 10000m, 1000m, 10, 202605, "Test"),
            default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("inactive");
    }

    [Fact]
    public async Task CreateEmployeeLoan_Success_And_Posts_Disbursement_Journal()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee();
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();

        var handler = new CreateEmployeeLoanCommandHandler(
            new Repository<EmployeeLoan, long>(ctx),
            new Repository<Employee>(ctx),
            new UnitOfWork(ctx),
            TestHarness.Numbering().Object,
            _journal.Object);

        var result = await handler.Handle(
            new CreateEmployeeLoanCommand(emp.Id, new DateOnly(2026, 5, 1), 24000m, 2000m, 12, 202605, "Advance", "BankTransfer"),
            default);

        result.Success.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);

        var loan = await ctx.EmployeeLoans.SingleAsync(l => l.Id == result.Data);
        loan.Status.Should().Be(EmployeeLoanStatus.Active);
        loan.Principal.Should().Be(24000m);
        loan.OutstandingPrincipal.Should().Be(24000m);
        loan.Code.Should().StartWith("LN-");

        // Verify GL Journal Call: Dr 1190 (EmployeeLoans) 24,000 / Cr 1020 (Bank) 24,000
        _journalLines.Should().Contain(l => l.Account == LedgerAccounts.EmployeeLoans && l.Debit == 24000m && l.Credit == 0m);
        _journalLines.Should().Contain(l => l.Account == LedgerAccounts.Bank && l.Debit == 0m && l.Credit == 24000m);
    }

    [Fact]
    public async Task UpdateEmployeeLoan_Updates_Active_Loan_And_Rejects_Closed_Loan()
    {
        await using var ctx = TestHarness.NewContext();
        var loan = new EmployeeLoan
        {
            Code = "LN-001",
            EmployeeId = 1,
            Principal = 10000m,
            OutstandingPrincipal = 10000m,
            EmiAmount = 1000m,
            TenureMonths = 10,
            StartYearMonth = 202605,
            Status = EmployeeLoanStatus.Active
        };
        ctx.EmployeeLoans.Add(loan);
        await ctx.SaveChangesAsync();

        var handler = new UpdateEmployeeLoanCommandHandler(
            new Repository<EmployeeLoan, long>(ctx),
            new UnitOfWork(ctx));

        // Update active loan terms
        var updateResult = await handler.Handle(
            new UpdateEmployeeLoanCommand(loan.Id, 2000m, 5, 202606, "Adjusted tenure"),
            default);

        updateResult.Success.Should().BeTrue();
        loan.EmiAmount.Should().Be(2000m);
        loan.TenureMonths.Should().Be(5);
        loan.StartYearMonth.Should().Be(202606);

        // Close loan and verify update rejection
        loan.Status = EmployeeLoanStatus.Closed;
        await ctx.SaveChangesAsync();

        var rejectedUpdate = await handler.Handle(
            new UpdateEmployeeLoanCommand(loan.Id, 1500m, 6, 202606, "Should fail"),
            default);

        rejectedUpdate.Success.Should().BeFalse();
        rejectedUpdate.Message.Should().Contain("Closed");
    }

    [Fact]
    public async Task CloseEmployeeLoan_Writes_Off_Remaining_Balance_To_GL()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee();
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();

        var loan = new EmployeeLoan
        {
            Code = "LN-WRITE-OFF",
            EmployeeId = emp.Id,
            Principal = 10000m,
            OutstandingPrincipal = 4000m,
            EmiAmount = 1000m,
            TenureMonths = 10,
            StartYearMonth = 202601,
            Status = EmployeeLoanStatus.Active,
            IsGlPosted = true
        };
        ctx.EmployeeLoans.Add(loan);
        await ctx.SaveChangesAsync();

        var handler = new CloseEmployeeLoanCommandHandler(
            new Repository<EmployeeLoan, long>(ctx),
            new Repository<Employee>(ctx),
            new UnitOfWork(ctx),
            _journal.Object);

        // Cancel / write-off remaining 4,000
        var result = await handler.Handle(new CloseEmployeeLoanCommand(loan.Id, Cancel: true), default);

        result.Success.Should().BeTrue();
        loan.Status.Should().Be(EmployeeLoanStatus.Cancelled);
        loan.OutstandingPrincipal.Should().Be(0m);

        // Verify write-off journal: Dr EmployerPfGratuity (5020) 4,000 / Cr EmployeeLoans (1190) 4,000
        _journalLines.Should().Contain(l => l.Account == LedgerAccounts.EmployerPfGratuity && l.Debit == 4000m);
        _journalLines.Should().Contain(l => l.Account == LedgerAccounts.EmployeeLoans && l.Credit == 4000m);
    }

    // =========================================================================
    // 3. PAYROLL INTEGRATION: LOAN AUTO-DEDUCTION & AUTO-CLOSING
    // =========================================================================

    [Fact]
    public async Task GeneratePayroll_Deducts_Active_Loan_And_AutoCloses_When_Fully_Repaid()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee(basic: 30000m);
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();

        // Active loan: 4,000 principal, 2,000 EMI
        var loan = new EmployeeLoan
        {
            Code = "LN-PAYROLL-1",
            EmployeeId = emp.Id,
            Principal = 4000m,
            OutstandingPrincipal = 4000m,
            EmiAmount = 2000m,
            TenureMonths = 2,
            StartYearMonth = 202605,
            Status = EmployeeLoanStatus.Active
        };
        ctx.EmployeeLoans.Add(loan);

        // Attendance for Karim
        ctx.AttendanceRecords.Add(new AttendanceRecord
        {
            EmployeeId = emp.Id,
            AttendanceDate = new DateOnly(2026, 5, 1),
            Status = AttendanceStatus.Present
        });
        await ctx.SaveChangesAsync();

        var payrollHandler = new GeneratePayrollCommandHandler(
            new Repository<Payslip, long>(ctx),
            new Repository<Employee>(ctx),
            new Repository<AttendanceRecord, long>(ctx),
            new Repository<EmployeeLoan, long>(ctx),
            new UnitOfWork(ctx));

        // Month 1: May 2026
        var run1 = await payrollHandler.Handle(new GeneratePayrollCommand(2026, 5), default);
        run1.Success.Should().BeTrue();

        var slip1 = await ctx.Payslips.SingleAsync(p => p.Year == 2026 && p.Month == 5);
        slip1.LoanDeduction.Should().Be(2000m);

        loan.OutstandingPrincipal.Should().Be(2000m);
        loan.Status.Should().Be(EmployeeLoanStatus.Active);

        // Month 2: June 2026 — Second EMI of 2,000 should clear the loan and auto-close it
        var run2 = await payrollHandler.Handle(new GeneratePayrollCommand(2026, 6), default);
        run2.Success.Should().BeTrue();

        var slip2 = await ctx.Payslips.SingleAsync(p => p.Year == 2026 && p.Month == 6);
        slip2.LoanDeduction.Should().Be(2000m);

        loan.OutstandingPrincipal.Should().Be(0m);
        loan.Status.Should().Be(EmployeeLoanStatus.Closed);
    }

    // =========================================================================
    // 4. FESTIVAL BONUS LIFECYCLE & DUPLICATE PREVENTION
    // =========================================================================

    [Fact]
    public async Task BulkCreateFestivalBonus_Creates_Drafts_And_Prevents_Duplicates()
    {
        await using var ctx = TestHarness.NewContext();
        var emp1 = ActiveEmployee("EMP-01");
        var emp2 = ActiveEmployee("EMP-02");
        ctx.Employees.AddRange(emp1, emp2);
        await ctx.SaveChangesAsync();

        var handler = new BulkCreateFestivalBonusCommandHandler(
            new Repository<FestivalBonus, long>(ctx),
            new Repository<Employee>(ctx),
            new UnitOfWork(ctx),
            TestHarness.Numbering().Object);

        // 1st Run: Issue Eid-ul-Fitr 2026 bonus of 15,000
        var run1 = await handler.Handle(
            new BulkCreateFestivalBonusCommand(2026, "EidUlFitr", 15000m, "Eid Bonus 2026"),
            default);

        run1.Success.Should().BeTrue();
        run1.Data.Should().Be(2);

        var bonuses = await ctx.FestivalBonuses.ToListAsync();
        bonuses.Should().HaveCount(2);
        bonuses.All(b => b.Status == FestivalBonusStatus.Draft && b.Amount == 15000m).Should().BeTrue();

        // 2nd Run: Re-issuing the exact same festival bonus should detect all employees already have it
        var run2 = await handler.Handle(
            new BulkCreateFestivalBonusCommand(2026, "EidUlFitr", 15000m, "Duplicate attempt"),
            default);

        run2.Success.Should().BeFalse();
        run2.Message.Should().Contain("already have a bonus");
    }

    [Fact]
    public async Task PayFestivalBonus_Success_And_Posts_SalaryExpense_Journal()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee("EMP-PAY-1");
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();

        var bonus = new FestivalBonus
        {
            Code = "FB-0001",
            EmployeeId = emp.Id,
            BonusYear = 2026,
            BonusType = FestivalBonusType.EidUlFitr,
            Amount = 18000m,
            Status = FestivalBonusStatus.Draft,
            PaymentMethod = PaymentMethod.BankTransfer
        };
        ctx.FestivalBonuses.Add(bonus);
        await ctx.SaveChangesAsync();

        var handler = new PayFestivalBonusCommandHandler(
            new Repository<FestivalBonus, long>(ctx),
            new UnitOfWork(ctx),
            new StubCurrentUser { UserName = "HROfficer" },
            _journal.Object);

        var result = await handler.Handle(new PayFestivalBonusCommand(bonus.Id), default);

        result.Success.Should().BeTrue();
        bonus.Status.Should().Be(FestivalBonusStatus.Paid);
        bonus.PaidBy.Should().Be("HROfficer");
        bonus.PaidAt.Should().NotBeNull();

        // Verify Journal Call: Dr 5010 (SalaryExpense) 18,000 / Cr 1020 (Bank) 18,000
        _journalLines.Should().Contain(l => l.Account == LedgerAccounts.SalaryExpense && l.Debit == 18000m);
        _journalLines.Should().Contain(l => l.Account == LedgerAccounts.Bank && l.Credit == 18000m);

        // Attempting to pay an already Paid bonus must fail
        var repeatPay = await handler.Handle(new PayFestivalBonusCommand(bonus.Id), default);
        repeatPay.Success.Should().BeFalse();
        repeatPay.Message.Should().Contain("Paid");
    }

    [Fact]
    public async Task DeleteFestivalBonus_Allows_Draft_And_Rejects_Paid()
    {
        await using var ctx = TestHarness.NewContext();
        var emp = ActiveEmployee("EMP-DEL-1");
        ctx.Employees.Add(emp);
        await ctx.SaveChangesAsync();

        var draftBonus = new FestivalBonus
        {
            Code = "FB-DRAFT",
            EmployeeId = emp.Id,
            BonusYear = 2026,
            BonusType = FestivalBonusType.PohelaBoishakh,
            Amount = 5000m,
            Status = FestivalBonusStatus.Draft
        };
        var paidBonus = new FestivalBonus
        {
            Code = "FB-PAID",
            EmployeeId = emp.Id,
            BonusYear = 2026,
            BonusType = FestivalBonusType.EidUlAzha,
            Amount = 10000m,
            Status = FestivalBonusStatus.Paid
        };
        ctx.FestivalBonuses.AddRange(draftBonus, paidBonus);
        await ctx.SaveChangesAsync();

        var handler = new DeleteFestivalBonusCommandHandler(
            new Repository<FestivalBonus, long>(ctx),
            new UnitOfWork(ctx));

        // Draft deletion succeeds
        var delDraft = await handler.Handle(new DeleteFestivalBonusCommand(draftBonus.Id), default);
        delDraft.Success.Should().BeTrue();
        (await ctx.FestivalBonuses.FindAsync(draftBonus.Id)).Should().BeNull();

        // Paid deletion fails
        var delPaid = await handler.Handle(new DeleteFestivalBonusCommand(paidBonus.Id), default);
        delPaid.Success.Should().BeFalse();
        delPaid.Message.Should().Contain("Paid");
    }
}

