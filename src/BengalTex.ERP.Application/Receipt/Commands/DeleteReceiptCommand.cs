using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Receipt.Commands;

/// <summary>
/// Soft-deletes a Receipt. A <b>Posted</b> receipt cannot be deleted — it has affected the
/// invoice balance and the ledger, so it must be <c>Cancel</c>led first (which reverses both).
/// Draft and already-cancelled receipts have no invoice/ledger effect and are removed directly.
/// (Reversal logic lives solely in <c>CancelReceiptCommand</c> — no duplication here.)
/// </summary>
public sealed record DeleteReceiptCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteReceiptCommandHandler : IRequestHandler<DeleteReceiptCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Receipt, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteReceiptCommandHandler(
        IRepository<Domain.Entities.Receipt, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var rct = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (rct is null) return ApiResponse.Fail("Receipt not found.");

        if (rct.Status == Domain.Entities.ReceiptStatus.Posted)
            return ApiResponse.Fail(
                "A posted receipt cannot be deleted — cancel it first to reverse the invoice balance.");

        _repo.Remove(rct);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Receipt deleted.");
    }
}
