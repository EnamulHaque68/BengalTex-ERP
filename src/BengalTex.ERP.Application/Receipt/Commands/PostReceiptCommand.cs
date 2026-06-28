using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Receipt.Dtos;
using BengalTex.ERP.Application.Receipt.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Receipt.Commands;

/// <summary>
/// Posts a Draft <see cref="Domain.Entities.Receipt"/> — the moment the money is recognised.
/// Atomic in one SaveChanges:
///   1. Validate the receipt is Draft and its invoice is Issued / PartiallyPaid.
///   2. Authoritative over-payment guard (Σ posted AmountPaid + Amount ≤ TotalAmount).
///   3. Flip the receipt to Posted (stamp PostedAt/PostedBy).
///   4. Increment <see cref="Domain.Entities.CustomerInvoice.AmountPaid"/> and recompute the
///      invoice status (PartiallyPaid if &lt; Total, Paid if &gt;= Total).
///   5. Post the cash/AR journal (with realized FX gain/loss when the receipt rate differs).
/// A Draft receipt never changes the invoice — only this command does.
/// </summary>
public sealed record PostReceiptCommand(long Id) : IRequest<ApiResponse<ReceiptDto>>;

internal sealed class PostReceiptCommandHandler
    : IRequestHandler<PostReceiptCommand, ApiResponse<ReceiptDto>>
{
    private readonly IRepository<Domain.Entities.Receipt, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public PostReceiptCommandHandler(
        IRepository<Domain.Entities.Receipt, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
        _currentUser = currentUser;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ReceiptDto>> Handle(
        PostReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var rct = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (rct is null) return ApiResponse<ReceiptDto>.Fail("Receipt not found.");
        if (rct.Status != Domain.Entities.ReceiptStatus.Draft)
            return ApiResponse<ReceiptDto>.Fail("Only draft receipts can be posted.");

        var inv = await _invRepo.GetByIdAsync(rct.CustomerInvoiceId, cancellationToken);
        if (inv is null) return ApiResponse<ReceiptDto>.Fail("Parent customer invoice not found.");

        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Issued &&
            inv.Status != Domain.Entities.CustomerInvoiceStatus.PartiallyPaid)
        {
            return ApiResponse<ReceiptDto>.Fail(
                "Receipts can only be posted against Issued or partially-paid invoices.");
        }

        var newAmountPaid = inv.AmountPaid + rct.Amount;
        if (newAmountPaid > inv.TotalAmount)
        {
            var outstanding = inv.TotalAmount - inv.AmountPaid;
            return ApiResponse<ReceiptDto>.Fail(
                $"Amount would overpay the invoice (outstanding {outstanding:0.####}, " +
                $"attempted {rct.Amount:0.####}).");
        }

        rct.Status = Domain.Entities.ReceiptStatus.Posted;
        rct.PostedAt = DateTimeOffset.UtcNow;
        rct.PostedBy = _currentUser.UserName;
        _repo.Update(rct);

        inv.AmountPaid = newAmountPaid;
        inv.Status = newAmountPaid >= inv.TotalAmount
            ? Domain.Entities.CustomerInvoiceStatus.Paid
            : Domain.Entities.CustomerInvoiceStatus.PartiallyPaid;
        _invRepo.Update(inv);

        await _uow.SaveChangesAsync(cancellationToken);

        // Auto-journal: Dr Cash/Bank at the RECEIPT rate (actual BDT in), Cr Accounts Receivable
        // at the INVOICE rate (the booked receivable being cleared). Any difference is a realized
        // FX gain (received more BDT than booked) or loss (received less).
        var cashAccount = rct.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
        var cashBdt = rct.Amount * rct.ExchangeRate;
        var arBdt = rct.Amount * inv.ExchangeRate;
        var fxDiff = cashBdt - arBdt;

        var lines = new List<JournalPostingLine>
        {
            new(cashAccount, cashBdt, 0m),
            new(LedgerAccounts.AccountsReceivable, 0m, arBdt),
        };
        if (fxDiff > 0m) lines.Add(new(LedgerAccounts.ExchangeGain, 0m, fxDiff));
        else if (fxDiff < 0m) lines.Add(new(LedgerAccounts.ExchangeLoss, -fxDiff, 0m));

        await _journal.PostAsync(
            rct.ReceiptDate, $"Receipt {rct.Code} against {inv.Code}", "Receipt", rct.Id, rct.Code,
            lines, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetReceiptByIdQuery(rct.Id), cancellationToken);
    }
}
