using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Issues a Draft customer invoice — locks the lines/total and makes it eligible for
/// receipts. Lifecycle: Draft → Issued. Once Issued, lines cannot be edited; only
/// AmountPaid/Status change via Receipt create/delete.
///
/// On Issue, if <c>VatAmount &gt; 0</c>, auto-creates a 1-to-1 <c>VatChallan</c> with
/// an auto-numbered "VC" code (BTX/VC/YYYY/00001). The challan snapshots the
/// invoice's Subtotal/VAT/Total at issue time and is legally immutable.
/// </summary>
public sealed record IssueCustomerInvoiceCommand(long Id) : IRequest<ApiResponse<CustomerInvoiceDto>>;

internal sealed class IssueCustomerInvoiceCommandHandler
    : IRequestHandler<IssueCustomerInvoiceCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.VatChallan, long> _challanRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public IssueCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.VatChallan, long> challanRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _challanRepo = challanRepo;
        _uow = uow;
        _currentUser = currentUser;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        IssueCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Customer invoice not found.");
        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Draft)
            return ApiResponse<CustomerInvoiceDto>.Fail("Only draft customer invoices can be issued.");
        if (inv.Lines.Count == 0)
            return ApiResponse<CustomerInvoiceDto>.Fail("Cannot issue an invoice with no lines.");

        // Recompute snapshot at issue time (already kept in sync by Create/Update, defensive)
        inv.SubtotalAmount = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        inv.VatAmount = Math.Round(inv.SubtotalAmount * inv.VatRate, 4, MidpointRounding.AwayFromZero);
        inv.TotalAmount = inv.SubtotalAmount + inv.VatAmount;

        inv.Status = Domain.Entities.CustomerInvoiceStatus.Issued;
        inv.IssuedAt = DateTimeOffset.UtcNow;
        inv.IssuedBy = _currentUser.UserName;

        // Auto-create VAT Challan when VatAmount > 0. Bangladesh NBR requires a Mushok 6.3
        // form (challan) accompanying every VAT-able sale. VAT-exempt invoices (rate = 0)
        // don't get one.
        if (inv.VatAmount > 0m)
        {
            var challanCode = await _numbering.NextAsync("VC", null, cancellationToken);
            var challan = new Domain.Entities.VatChallan
            {
                Code = challanCode,
                CustomerInvoiceId = inv.Id,
                ChallanDate = inv.InvoiceDate,
                SubtotalAmount = inv.SubtotalAmount,
                VatAmount = inv.VatAmount,
                TotalAmount = inv.TotalAmount,
                Notes = null
            };
            await _challanRepo.AddAsync(challan, cancellationToken);
        }

        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
