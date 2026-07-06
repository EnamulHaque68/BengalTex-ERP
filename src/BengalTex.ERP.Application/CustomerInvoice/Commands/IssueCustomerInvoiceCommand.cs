using BengalTex.ERP.Application.Accounting;
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
    private readonly IRepository<Domain.Entities.SalesOrderLine, long> _soLineRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly INumberingService _numbering;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public IssueCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.VatChallan, long> challanRepo,
        IRepository<Domain.Entities.SalesOrderLine, long> soLineRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        INumberingService numbering,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _challanRepo = challanRepo;
        _soLineRepo = soLineRepo;
        _uow = uow;
        _currentUser = currentUser;
        _numbering = numbering;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        IssueCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(c => c.Lines).ThenInclude(l => l.Product)
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

        // Snapshot cost-at-sale per line (Product WAC, base BDT) so the margin report stays
        // historically accurate as the live WAC drifts after this sale.
        foreach (var line in inv.Lines)
            line.UnitCostAtSale = line.Product.WeightedAverageCost;

        inv.Status = Domain.Entities.CustomerInvoiceStatus.Issued;
        inv.IssuedAt = DateTimeOffset.UtcNow;
        inv.IssuedBy = _currentUser.UserName;

        // Auto-create VAT Challan when VatAmount > 0. Bangladesh NBR requires a Mushok 6.3
        // form (challan) accompanying every VAT-able sale. VAT-exempt invoices (rate = 0)
        // don't get one. Opening invoices (Phase A1) are historic go-live documents — no challan.
        if (inv.VatAmount > 0m && !inv.IsOpening)
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

        // Auto-journal (base BDT = doc amount × exchange rate). Phase A3 — dimensioned & split
        // per line so per-style/buyer P&L is exact:
        //   Dr Accounts Receivable (gross)          [BUYER, ORDER]
        //   Cr Sales Revenue — one leg per invoice line  [BUYER, STYLE, ORDER]
        //   Cr VAT Payable (output VAT)              [ORDER]
        // Phase A1: SUPPRESSED for opening invoices — their GL value lives on the opening-balance
        // voucher; posting here would double-count AR and revenue. Receipts/ageing still work.
        if (!inv.IsOpening)
        {
            var rate = inv.ExchangeRate;

            // Resolve each invoice line's style via its linked SO line (buyer & order are header-level).
            var soLineIds = inv.Lines.Where(l => l.SalesOrderLineId.HasValue)
                .Select(l => l.SalesOrderLineId!.Value).Distinct().ToList();
            var styleBySoLine = soLineIds.Count == 0
                ? new Dictionary<long, int?>()
                : await _soLineRepo.Query().Where(sl => soLineIds.Contains(sl.Id))
                    .ToDictionaryAsync(sl => sl.Id, sl => sl.StyleId, cancellationToken);

            var buyer = inv.CustomerId;
            var order = inv.SalesOrderId;
            var legs = new List<JournalPostingLine>();

            // Credit legs first (pre-rounded per line) so the AR debit is their exact sum → always balanced.
            decimal creditTotal = 0m;
            foreach (var line in inv.Lines)
            {
                var net = Math.Round(line.Quantity * line.UnitPrice * rate, 2, MidpointRounding.AwayFromZero);
                if (net == 0m) continue;
                int? style = line.SalesOrderLineId.HasValue && styleBySoLine.TryGetValue(line.SalesOrderLineId.Value, out var s) ? s : null;
                legs.Add(new JournalPostingLine(LedgerAccounts.SalesRevenue, 0m, net,
                    new Dimensions(BuyerId: buyer, StyleId: style, SalesOrderId: order)));
                creditTotal += net;
            }
            var vat = Math.Round(inv.VatAmount * rate, 2, MidpointRounding.AwayFromZero);
            if (vat != 0m)
            {
                legs.Add(new JournalPostingLine(LedgerAccounts.VatPayable, 0m, vat,
                    new Dimensions(SalesOrderId: order)));
                creditTotal += vat;
            }
            legs.Insert(0, new JournalPostingLine(LedgerAccounts.AccountsReceivable, creditTotal, 0m,
                new Dimensions(BuyerId: buyer, SalesOrderId: order)));

            await _journal.PostAsync(
                inv.InvoiceDate, $"Sales invoice {inv.Code}", "CustomerInvoice", inv.Id, inv.Code,
                legs, cancellationToken);
        }

        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
