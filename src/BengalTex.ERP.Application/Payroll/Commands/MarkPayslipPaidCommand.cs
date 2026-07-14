using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Application.Payroll.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Commands;

/// <summary>
/// Phase A5 — marks an Approved payslip Paid (immutable thereafter) and posts the net-pay
/// settlement. The gross was already expensed and deductions moved to payables at Approve
/// (see <see cref="ApprovePayslipCommand"/>), so payment only clears the net Salary Payable:
///   Dr Salary Payable (2130)  / Cr Cash (1110) or Bank (1120) by chosen <see cref="PaymentMethod"/>.
/// Amount = <c>p.NetPay</c>. Defaults to BankTransfer if unspecified.
/// </summary>
public sealed record MarkPayslipPaidCommand(long Id, string? PaymentMethod = "BankTransfer")
    : IRequest<ApiResponse<PayslipDto>>;

public sealed class MarkPayslipPaidCommandValidator : AbstractValidator<MarkPayslipPaidCommand>
{
    public MarkPayslipPaidCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.PaymentMethod)
            .Must(s => string.IsNullOrEmpty(s) || Enum.TryParse<PaymentMethod>(s, out _))
            .WithMessage("PaymentMethod must be Cash, BankTransfer, Cheque, MobileBanking, or Other.");
    }
}

internal sealed class MarkPayslipPaidCommandHandler
    : IRequestHandler<MarkPayslipPaidCommand, ApiResponse<PayslipDto>>
{
    private readonly IRepository<Payslip, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public MarkPayslipPaidCommandHandler(
        IRepository<Payslip, long> repo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _empRepo = empRepo;
        _uow = uow;
        _currentUser = currentUser;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PayslipDto>> Handle(MarkPayslipPaidCommand cmd, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(cmd.Id, ct);
        if (p is null) return ApiResponse<PayslipDto>.Fail("Payslip not found.");
        if (p.Status == PayslipStatus.Paid)
            return ApiResponse<PayslipDto>.Fail("Payslip is already marked paid.");
        if (p.Status != PayslipStatus.Approved)
            return ApiResponse<PayslipDto>.Fail("Approve the payslip first to accrue the salary, then mark it paid.");

        p.Status = PayslipStatus.Paid;
        p.PaidAt = DateTimeOffset.UtcNow;
        p.PaidBy = _currentUser.UserName;
        _repo.Update(p);

        // Net-pay settlement — the accrual already expensed the gross and raised the payables,
        // so payment only clears the net Salary Payable against Cash|Bank.
        if (p.NetPay > 0m)
        {
            var method = string.IsNullOrEmpty(cmd.PaymentMethod)
                ? PaymentMethod.BankTransfer
                : Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
            var cashAccount = method == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;

            var emp = await _empRepo.Query().AsNoTracking().Where(e => e.Id == p.EmployeeId)
                .Select(e => new { e.Code, e.FullName }).FirstOrDefaultAsync(ct);
            var empLabel = emp is null ? $"Emp #{p.EmployeeId}" : $"{emp.FullName} ({emp.Code})";

            var payDate = DateOnly.FromDateTime(p.PaidAt!.Value.UtcDateTime);
            await _journal.PostAsync(
                payDate,
                $"Salary paid {p.Year}-{p.Month:D2} — {empLabel}",
                "Payslip", p.Id, $"PS-{p.Year}{p.Month:D2}-{p.EmployeeId}",
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.SalaryPayable, p.NetPay, 0m),
                    new JournalPostingLine(cashAccount, 0m, p.NetPay),
                }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetPayslipByIdQuery(p.Id), ct);
    }
}
