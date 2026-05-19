using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Payment.Commands;

/// <summary>
/// Deletes a Payment and reverses its impact on the parent invoice. Atomic in one
/// SaveChanges:
///   1. Reverse <see cref="Domain.Entities.SupplierInvoice.AmountPaid"/> by the
///      payment's amount.
///   2. Recompute invoice status — Approved if no payments remain, PartiallyPaid if
///      &gt; 0 and &lt; Total, Paid if still &gt;= Total.
///   3. Soft-delete the payment.
/// </summary>
public sealed record DeletePaymentCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeletePaymentCommandHandler : IRequestHandler<DeletePaymentCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Payment, long> _repo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;

    public DeletePaymentCommandHandler(
        IRepository<Domain.Entities.Payment, long> repo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeletePaymentCommand cmd, CancellationToken cancellationToken)
    {
        var pay = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (pay is null) return ApiResponse.Fail("Payment not found.");

        var inv = await _invRepo.GetByIdAsync(pay.SupplierInvoiceId, cancellationToken);
        if (inv is null) return ApiResponse.Fail("Parent supplier invoice not found.");

        var newAmountPaid = inv.AmountPaid - pay.Amount;
        if (newAmountPaid < 0m) newAmountPaid = 0m;   // defensive — shouldn't happen but clamp

        inv.AmountPaid = newAmountPaid;
        inv.Status = newAmountPaid >= inv.TotalAmount
            ? Domain.Entities.SupplierInvoiceStatus.Paid
            : (newAmountPaid > 0m
                ? Domain.Entities.SupplierInvoiceStatus.PartiallyPaid
                : Domain.Entities.SupplierInvoiceStatus.Approved);
        _invRepo.Update(inv);

        _repo.Remove(pay);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Payment deleted and invoice balance reversed.");
    }
}
