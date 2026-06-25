using BengalTex.ERP.Application.ProformaInvoices.Commands;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Quotations.Commands;

/// <summary>
/// Generates a draft Proforma Invoice from an Accepted quotation (one proforma line per quotation line,
/// at the quoted price). Pre-order flow: Quotation → Proforma → (customer confirmation) → Sales Order.
/// Rule: only ONE active proforma per quotation; can't generate if the quotation already became a Sales Order.
/// </summary>
public sealed record GenerateProformaFromQuotationCommand(long QuotationId) : IRequest<ApiResponse<long>>;

internal sealed class GenerateProformaFromQuotationCommandHandler
    : IRequestHandler<GenerateProformaFromQuotationCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _proformaRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public GenerateProformaFromQuotationCommandHandler(
        IRepository<Domain.Entities.Quotation, long> repo,
        IRepository<Domain.Entities.ProformaInvoice, long> proformaRepo,
        IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _proformaRepo = proformaRepo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<long>> Handle(GenerateProformaFromQuotationCommand cmd, CancellationToken ct)
    {
        var q = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.QuotationId, ct);
        if (q is null) return ApiResponse<long>.Fail("Quotation not found.");
        if (q.Status != QuotationStatus.Accepted)
            return ApiResponse<long>.Fail("Only an accepted quotation can generate a proforma invoice.");
        if (q.ConvertedSalesOrderId.HasValue)
            return ApiResponse<long>.Fail("This quotation already has a sales order — a proforma can't be generated.");

        // Rule: one active (non-cancelled/expired) proforma per quotation.
        var hasActiveProforma = await _proformaRepo.Query().AnyAsync(
            p => p.QuotationId == q.Id
                 && p.Status != ProformaInvoiceStatus.Cancelled
                 && p.Status != ProformaInvoiceStatus.Expired, ct);
        if (hasActiveProforma)
            return ApiResponse<long>.Fail("An active proforma invoice already exists for this quotation.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lines = q.Lines.OrderBy(l => l.SortOrder)
            .Select(l => new ProformaInvoiceLineInput(l.ProductId, l.Quantity, l.UnitPrice, l.Description))
            .ToList();

        // VAT defaults to 15% (BD standard); it's editable on the draft proforma before sending.
        var createResult = await _mediator.Send(new CreateProformaInvoiceCommand(
            CustomerId: q.CustomerId,
            SalesOrderId: null,
            IssueDate: today,
            ValidUntil: q.ValidUntil ?? today.AddDays(15),
            CurrencyId: q.CurrencyId,
            ExchangeRate: q.ExchangeRate,
            VatRate: 0.15m,
            Notes: $"Generated from quotation {q.Code}",
            Lines: lines,
            QuotationId: q.Id), ct);

        if (!createResult.Success || createResult.Data == 0)
            return ApiResponse<long>.Fail(createResult.Message ?? "Could not generate the proforma invoice.");

        q.ConvertedProformaInvoiceId = createResult.Data;
        _repo.Update(q);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(createResult.Data, $"Proforma generated from quotation {q.Code}.");
    }
}
