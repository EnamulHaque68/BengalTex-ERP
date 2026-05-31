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
/// Marks a Draft payslip as Paid (immutable thereafter) AND auto-journals the payment:
///   Dr Salary Expense (5200)  / Cr Cash (1110) or Bank (1120) by chosen <see cref="PaymentMethod"/>.
/// Amount = <c>p.NetPay</c>. v1a defaults to BankTransfer if unspecified; v1b will split into
/// gross-vs-net (Salary Expense + Salary Payable + PF + Income Tax) for full payroll accounting.
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

        p.Status = PayslipStatus.Paid;
        p.PaidAt = DateTimeOffset.UtcNow;
        p.PaidBy = _currentUser.UserName;
        _repo.Update(p);

        // Auto-journal: Dr Salary Expense / Cr Cash|Bank for NetPay
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
                    new JournalPostingLine(LedgerAccounts.SalaryExpense, p.NetPay, 0m),
                    new JournalPostingLine(cashAccount, 0m, p.NetPay),
                }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetPayslipByIdQuery(p.Id), ct);
    }
}
