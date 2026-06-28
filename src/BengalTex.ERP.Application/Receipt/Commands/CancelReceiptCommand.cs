using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Receipt.Dtos;
using BengalTex.ERP.Application.Receipt.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Receipt.Commands;

/// <summary>
/// Cancels a <see cref="Domain.Entities.Receipt"/>:
///   • A <b>Draft</b> is simply voided — nothing to reverse (the invoice was never touched).
///   • A <b>Posted</b> receipt is reversed — the invoice's <c>AmountPaid</c> is debited by the
///     receipt amount, its status recomputed, and a mirror journal is posted (Dr AR / Cr Cash,
///     reversing any realized FX too).
/// The receipt row is kept (status → Cancelled) for an auditable trail rather than deleted.
/// </summary>
public sealed record CancelReceiptCommand(long Id) : IRequest<ApiResponse<ReceiptDto>>;

internal sealed class CancelReceiptCommandHandler
    : IRequestHandler<CancelReceiptCommand, ApiResponse<ReceiptDto>>
{
    private readonly IRepository<Domain.Entities.Receipt, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public CancelReceiptCommandHandler(
        IRepository<Domain.Entities.Receipt, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ReceiptDto>> Handle(CancelReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var rct = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (rct is null) return ApiResponse<ReceiptDto>.Fail("Receipt not found.");
        if (rct.Status == Domain.Entities.ReceiptStatus.Cancelled)
            return ApiResponse<ReceiptDto>.Fail("This receipt is already cancelled.");

        var wasPosted = rct.Status == Domain.Entities.ReceiptStatus.Posted;

        // Reverse the invoice effect only for a posted receipt (a draft never affected it).
        if (wasPosted)
        {
            var inv = await _invRepo.GetByIdAsync(rct.CustomerInvoiceId, cancellationToken);
            if (inv is null) return ApiResponse<ReceiptDto>.Fail("Parent customer invoice not found.");

            var newAmountPaid = inv.AmountPaid - rct.Amount;
            if (newAmountPaid < 0m) newAmountPaid = 0m;   // defensive clamp

            inv.AmountPaid = newAmountPaid;
            inv.Status = newAmountPaid >= inv.TotalAmount
                ? Domain.Entities.CustomerInvoiceStatus.Paid
                : (newAmountPaid > 0m
                    ? Domain.Entities.CustomerInvoiceStatus.PartiallyPaid
                    : Domain.Entities.CustomerInvoiceStatus.Issued);
            _invRepo.Update(inv);

            rct.Status = Domain.Entities.ReceiptStatus.Cancelled;
            _repo.Update(rct);

            await _uow.SaveChangesAsync(cancellationToken);

            // Mirror journal of the original posting (Dr AR / Cr Cash, plus reversed FX).
            var cashAccount = rct.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
            var cashBdt = rct.Amount * rct.ExchangeRate;
            var arBdt = rct.Amount * inv.ExchangeRate;
            var fxDiff = cashBdt - arBdt;

            var lines = new List<JournalPostingLine>
            {
                new(LedgerAccounts.AccountsReceivable, arBdt, 0m),
                new(cashAccount, 0m, cashBdt),
            };
            if (fxDiff > 0m) lines.Add(new(LedgerAccounts.ExchangeGain, fxDiff, 0m));
            else if (fxDiff < 0m) lines.Add(new(LedgerAccounts.ExchangeLoss, 0m, -fxDiff));

            await _journal.PostAsync(
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"Cancellation of receipt {rct.Code}", "ReceiptCancellation", rct.Id, rct.Code,
                lines, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Draft → just void it.
            rct.Status = Domain.Entities.ReceiptStatus.Cancelled;
            _repo.Update(rct);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return await _mediator.Send(new GetReceiptByIdQuery(rct.Id), cancellationToken);
    }
}
