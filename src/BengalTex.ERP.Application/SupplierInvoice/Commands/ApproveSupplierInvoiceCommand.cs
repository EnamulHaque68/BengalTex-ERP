using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

/// <summary>
/// Approves a Draft supplier invoice — locks lines/total and authorizes it for
/// payment. Lifecycle: Draft → Approved. Once Approved, lines cannot be edited;
/// only AmountPaid/Status change via Payment create/delete. Mirror of
/// <c>IssueCustomerInvoiceCommand</c>; called "Approve" here because we're
/// internally authorizing the supplier's bill (we don't "issue" it).
/// </summary>
public sealed record ApproveSupplierInvoiceCommand(long Id) : IRequest<ApiResponse<SupplierInvoiceDto>>;

internal sealed class ApproveSupplierInvoiceCommandHandler
    : IRequestHandler<ApproveSupplierInvoiceCommand, ApiResponse<SupplierInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public ApproveSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        ApproveSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse<SupplierInvoiceDto>.Fail("Supplier invoice not found.");
        if (inv.Status != Domain.Entities.SupplierInvoiceStatus.Draft)
            return ApiResponse<SupplierInvoiceDto>.Fail("Only draft supplier invoices can be approved.");
        if (inv.Lines.Count == 0)
            return ApiResponse<SupplierInvoiceDto>.Fail("Cannot approve an invoice with no lines.");

        // Recompute subtotal/VAT/total defensively (already kept in sync by Create/Update)
        inv.SubtotalAmount = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        inv.VatAmount = Math.Round(inv.SubtotalAmount * inv.VatRate, 4, MidpointRounding.AwayFromZero);
        inv.TotalAmount = inv.SubtotalAmount + inv.VatAmount;

        inv.Status = Domain.Entities.SupplierInvoiceStatus.Approved;
        inv.ApprovedAt = DateTimeOffset.UtcNow;
        inv.ApprovedBy = _currentUser.UserName;

        // Auto-journal (base BDT = doc amount × exchange rate):
        //   Dr Raw Material Inventory (net) / Dr VAT Receivable (input VAT) / Cr Accounts Payable (gross)
        // Phase A1: SUPPRESSED for opening bills — their GL value lives on the opening-balance
        // voucher; posting here would double-count AP. Payments/ageing still work.
        if (!inv.IsOpening)
        {
            var rate = inv.ExchangeRate;
            await _journal.PostAsync(
                inv.InvoiceDate, $"Supplier bill {inv.Code}", "SupplierInvoice", inv.Id, inv.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.RawMaterialInventory, inv.SubtotalAmount * rate, 0m),
                    new JournalPostingLine(LedgerAccounts.VatReceivable, inv.VatAmount * rate, 0m),
                    new JournalPostingLine(LedgerAccounts.AccountsPayable, 0m, inv.TotalAmount * rate),
                }, cancellationToken);
        }

        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
