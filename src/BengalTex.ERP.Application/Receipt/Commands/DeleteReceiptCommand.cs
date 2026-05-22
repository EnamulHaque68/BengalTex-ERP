using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Receipt.Commands;

/// <summary>
/// Deletes a Receipt and reverses its impact on the parent invoice. Atomic in one
/// SaveChanges:
///   1. Reverse <see cref="Domain.Entities.CustomerInvoice.AmountPaid"/> by the
///      receipt's amount.
///   2. Recompute invoice status — Issued if no payments remain, PartiallyPaid if
///      &gt; 0 and &lt; Total, Paid if still &gt;= Total.
///   3. Soft-delete the receipt.
/// </summary>
public sealed record DeleteReceiptCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteReceiptCommandHandler : IRequestHandler<DeleteReceiptCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Receipt, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public DeleteReceiptCommandHandler(
        IRepository<Domain.Entities.Receipt, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        IJournalPostingService journal)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
        _journal = journal;
    }

    public async Task<ApiResponse> Handle(DeleteReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var rct = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (rct is null) return ApiResponse.Fail("Receipt not found.");

        var inv = await _invRepo.GetByIdAsync(rct.CustomerInvoiceId, cancellationToken);
        if (inv is null) return ApiResponse.Fail("Parent customer invoice not found.");

        var newAmountPaid = inv.AmountPaid - rct.Amount;
        if (newAmountPaid < 0m) newAmountPaid = 0m;   // defensive — shouldn't happen but clamp

        inv.AmountPaid = newAmountPaid;
        inv.Status = newAmountPaid >= inv.TotalAmount
            ? Domain.Entities.CustomerInvoiceStatus.Paid
            : (newAmountPaid > 0m
                ? Domain.Entities.CustomerInvoiceStatus.PartiallyPaid
                : Domain.Entities.CustomerInvoiceStatus.Issued);
        _invRepo.Update(inv);

        _repo.Remove(rct);

        // Reversing journal — mirror of the original receipt entry (Dr AR, Cr Cash/Bank).
        var cashAccount = rct.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
        var baseAmount = rct.Amount * inv.ExchangeRate;
        await _journal.PostAsync(
            DateOnly.FromDateTime(DateTime.UtcNow), $"Reversal of receipt {rct.Code}", "ReceiptReversal", rct.Id, rct.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.AccountsReceivable, baseAmount, 0m),
                new JournalPostingLine(cashAccount, 0m, baseAmount),
            }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Receipt deleted and invoice balance reversed.");
    }
}
