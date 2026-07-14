using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using BengalTex.ERP.Shared.Common;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A5a — People &amp; State (payroll gross accrual + loan + final-settlement GL). Verifies that
/// approving a payslip books the full earned gross as expense and moves each deduction to its own
/// payable, that payment then only clears the net Salary Payable, and that loans &amp; settlements post.
/// </summary>
public class PeopleStateTests
{
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1110", Name = "Cash", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "1120", Name = "Bank", AccountType = AccountType.Asset },
            new Account { Id = 3, Code = "1190", Name = "Employee Loans", AccountType = AccountType.Asset },
            new Account { Id = 4, Code = "2130", Name = "Salary Payable", AccountType = AccountType.Liability },
            new Account { Id = 5, Code = "2135", Name = "PF Payable", AccountType = AccountType.Liability },
            new Account { Id = 6, Code = "2160", Name = "AIT Payable", AccountType = AccountType.Liability },
            new Account { Id = 7, Code = "5200", Name = "Salary Expense", AccountType = AccountType.Expense },
            new Account { Id = 8, Code = "5210", Name = "Employer PF & Gratuity", AccountType = AccountType.Expense });
        ctx.SaveChanges();
    }

    private static JournalPostingService Posting(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            TestHarness.Numbering().Object, new StubCurrentUser(), new StubClock(),
            new PeriodGuard(ctx, new StubCurrentUser()));

    private static decimal Bal(ApplicationDbContext ctx, string code)
    {
        var accId = ctx.Accounts.Single(a => a.Code == code).Id;
        return ctx.JournalEntryLines.Where(l => l.AccountId == accId).Sum(l => l.Debit - l.Credit);
    }

    private static Employee ActiveEmployee(ApplicationDbContext ctx)
    {
        var e = new Employee
        {
            Code = "EMP-1", FullName = "Karim", BasicSalary = 30_000m,
            IsActive = true, Status = EmployeeStatus.Active,
            EmploymentType = EmploymentType.Permanent, Gender = Gender.Male,
            JoiningDate = new DateOnly(2020, 1, 1)
        };
        ctx.Employees.Add(e);
        ctx.SaveChanges();
        return e;
    }

    // ── E1 — payroll gross accrual ──

    [Fact]
    public async Task Approve_accrues_earned_gross_and_raises_each_deduction_payable()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var emp = ActiveEmployee(ctx);
        ctx.Payslips.Add(new Payslip
        {
            EmployeeId = emp.Id, Year = 2026, Month = 6, BasicSalary = 30_000m,
            GrossPay = 25_000m, NetPay = 20_000m,
            PfEmployee = 1_000m, PfEmployer = 1_000m, IncomeTax = 500m, LoanDeduction = 1_500m,
            Status = PayslipStatus.Draft
        });
        ctx.SaveChanges();
        var slip = ctx.Payslips.Single();

        var handler = new ApprovePayslipCommandHandler(
            new Repository<Payslip, long>(ctx), new Repository<Employee>(ctx),
            new Repository<CostCenter>(ctx), new UnitOfWork(ctx), new StubCurrentUser(), Posting(ctx));

        var res = await handler.Handle(new ApprovePayslipCommand(slip.Id), default);

        res.Success.Should().BeTrue();
        ctx.Payslips.Single().Status.Should().Be(PayslipStatus.Approved);

        // earned gross = net + pf-emp + tax + loan = 23,000 expensed; employer PF = 1,000 expensed.
        Bal(ctx, "5200").Should().Be(23_000m);
        Bal(ctx, "5210").Should().Be(1_000m);
        // deductions become payables / recover the loan receivable.
        Bal(ctx, "2130").Should().Be(-20_000m);   // net Salary Payable
        Bal(ctx, "2135").Should().Be(-2_000m);     // PF Payable = emp + employer
        Bal(ctx, "2160").Should().Be(-500m);       // AIT Payable
        Bal(ctx, "1190").Should().Be(-1_500m);     // Employee Loans reduced by recovery

        // Entry balances.
        var je = ctx.JournalEntries.Single(j => j.SourceType == "PayslipAccrual");
        je.Lines.Sum(l => l.Debit).Should().Be(je.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task Mark_paid_requires_approval_then_clears_salary_payable()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var emp = ActiveEmployee(ctx);
        ctx.Payslips.Add(new Payslip
        {
            EmployeeId = emp.Id, Year = 2026, Month = 6, GrossPay = 20_000m, NetPay = 20_000m,
            Status = PayslipStatus.Draft
        });
        ctx.SaveChanges();
        var slip = ctx.Payslips.Single();

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<Application.Payroll.Queries.GetPayslipByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<PayslipDto>.Ok(null!));
        var markPaid = new MarkPayslipPaidCommandHandler(
            new Repository<Payslip, long>(ctx), new Repository<Employee>(ctx),
            new UnitOfWork(ctx), new StubCurrentUser(), Posting(ctx), mediator.Object);

        // Draft can't be paid — must accrue first.
        (await markPaid.Handle(new MarkPayslipPaidCommand(slip.Id, "BankTransfer"), default))
            .Success.Should().BeFalse();

        // Approve, then pay.
        await new ApprovePayslipCommandHandler(
            new Repository<Payslip, long>(ctx), new Repository<Employee>(ctx),
            new Repository<CostCenter>(ctx), new UnitOfWork(ctx), new StubCurrentUser(), Posting(ctx))
            .Handle(new ApprovePayslipCommand(slip.Id), default);

        var paid = await markPaid.Handle(new MarkPayslipPaidCommand(slip.Id, "BankTransfer"), default);
        paid.Success.Should().BeTrue();
        ctx.Payslips.Single().Status.Should().Be(PayslipStatus.Paid);

        // Accrual (Cr 2130 20,000) + payment (Dr 2130 20,000) net the payable to zero; Bank credited.
        Bal(ctx, "2130").Should().Be(0m);
        Bal(ctx, "1120").Should().Be(-20_000m);
        Bal(ctx, "5200").Should().Be(20_000m);   // expensed once, at accrual — not double
    }

    // ── E2 — employee loan GL ──

    [Fact]
    public async Task Loan_disbursement_debits_receivable_against_bank()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var emp = ActiveEmployee(ctx);

        var handler = new CreateEmployeeLoanCommandHandler(
            new Repository<EmployeeLoan, long>(ctx), new Repository<Employee>(ctx),
            new UnitOfWork(ctx), TestHarness.Numbering().Object, Posting(ctx));

        var res = await handler.Handle(new CreateEmployeeLoanCommand(
            emp.Id, new DateOnly(2026, 6, 1), 50_000m, 5_000m, 10, 202607, null), default);

        res.Success.Should().BeTrue();
        var loan = ctx.EmployeeLoans.Single();
        loan.IsGlPosted.Should().BeTrue();
        loan.OutstandingPrincipal.Should().Be(50_000m);
        Bal(ctx, "1190").Should().Be(50_000m);    // Dr Employee Loans
        Bal(ctx, "1120").Should().Be(-50_000m);   // Cr Bank
    }

    [Fact]
    public async Task Closing_a_loan_with_a_balance_writes_off_the_receivable()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var emp = ActiveEmployee(ctx);
        ctx.EmployeeLoans.Add(new EmployeeLoan
        {
            Code = "LN-1", EmployeeId = emp.Id, IssuedDate = new DateOnly(2026, 1, 1),
            Principal = 50_000m, EmiAmount = 5_000m, TenureMonths = 10, StartYearMonth = 202601,
            OutstandingPrincipal = 30_000m, Status = EmployeeLoanStatus.Active, IsGlPosted = true
        });
        ctx.SaveChanges();
        var loan = ctx.EmployeeLoans.Single();

        var res = await new CloseEmployeeLoanCommandHandler(
            new Repository<EmployeeLoan, long>(ctx), new Repository<Employee>(ctx),
            new UnitOfWork(ctx), Posting(ctx))
            .Handle(new CloseEmployeeLoanCommand(loan.Id), default);

        res.Success.Should().BeTrue();
        ctx.EmployeeLoans.Single().Status.Should().Be(EmployeeLoanStatus.Closed);
        ctx.EmployeeLoans.Single().OutstandingPrincipal.Should().Be(0m);
        Bal(ctx, "5210").Should().Be(30_000m);   // written off to staff cost
        Bal(ctx, "1190").Should().Be(-30_000m);  // receivable cleared
    }

    // ── E3 — final settlement GL ──

    [Fact]
    public async Task Final_settlement_approve_then_pay_posts_expected_journals()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var emp = ActiveEmployee(ctx);
        ctx.FinalSettlements.Add(new FinalSettlement
        {
            Code = "FS-1", EmployeeId = emp.Id,
            SettlementDate = new DateOnly(2026, 6, 30), LastWorkingDate = new DateOnly(2026, 6, 30),
            JoiningDate = emp.JoiningDate, YearsOfService = 6m, Reason = SettlementReason.Resignation,
            BasicSalary = 30_000m, ProratedDays = 10m, ProratedSalary = 10_000m,
            LeaveEncashmentDays = 2m, LeaveEncashmentAmount = 2_000m, GratuityAmount = 30_000m,
            OtherEarnings = 0m, OutstandingLoan = 5_000m, OtherDeductions = 0m,
            GrossPayable = 42_000m, TotalDeductions = 5_000m, NetPayable = 37_000m,
            Status = FinalSettlementStatus.Draft
        });
        ctx.SaveChanges();
        var fs = ctx.FinalSettlements.Single();

        var approve = await new ApproveFinalSettlementCommandHandler(
            new Repository<FinalSettlement, long>(ctx), new Repository<Employee>(ctx),
            new Repository<CostCenter>(ctx), new UnitOfWork(ctx), new StubCurrentUser(), Posting(ctx))
            .Handle(new ApproveFinalSettlementCommand(fs.Id), default);
        approve.Success.Should().BeTrue();

        Bal(ctx, "5200").Should().Be(12_000m);    // prorated (10,000) + leave (2,000) + other (0)
        Bal(ctx, "5210").Should().Be(30_000m);    // gratuity
        Bal(ctx, "1190").Should().Be(-5_000m);    // loan recovered
        Bal(ctx, "2130").Should().Be(-37_000m);   // net payable raised

        var pay = await new MarkFinalSettlementPaidCommandHandler(
            new Repository<FinalSettlement, long>(ctx), new Repository<EmployeeLoan, long>(ctx),
            new UnitOfWork(ctx), Posting(ctx))
            .Handle(new MarkFinalSettlementPaidCommand(fs.Id, "BankTransfer", "TXN-1"), default);
        pay.Success.Should().BeTrue();

        Bal(ctx, "2130").Should().Be(0m);          // payable cleared
        Bal(ctx, "1120").Should().Be(-37_000m);    // bank paid out
    }
}
