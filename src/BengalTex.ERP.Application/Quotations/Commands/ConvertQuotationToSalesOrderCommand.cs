using BengalTex.ERP.Application.Quotations.Dtos;
using BengalTex.ERP.Application.Quotations.Queries;
using BengalTex.ERP.Application.SalesOrder.Commands;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Quotations.Commands;

/// <summary>
/// Converts an Accepted quotation into a draft Sales Order (one SO line per quotation line, at
/// the quoted unit price). Marks the quotation Converted and records the new Sales Order id.
/// </summary>
public sealed record ConvertQuotationToSalesOrderCommand(long Id) : IRequest<ApiResponse<QuotationDto>>;

internal sealed class ConvertQuotationToSalesOrderCommandHandler
    : IRequestHandler<ConvertQuotationToSalesOrderCommand, ApiResponse<QuotationDto>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _proformaRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ConvertQuotationToSalesOrderCommandHandler(
        IRepository<Domain.Entities.Quotation, long> repo,
        IRepository<Domain.Entities.ProformaInvoice, long> proformaRepo,
        IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _proformaRepo = proformaRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QuotationDto>> Handle(ConvertQuotationToSalesOrderCommand cmd, CancellationToken ct)
    {
        var q = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (q is null) return ApiResponse<QuotationDto>.Fail("Quotation not found.");
        if (q.Status != QuotationStatus.Accepted)
            return ApiResponse<QuotationDto>.Fail("Only an accepted quotation can be converted to a sales order.");

        // Rule: at most one active Sales Order per quotation.
        if (q.ConvertedSalesOrderId.HasValue)
            return ApiResponse<QuotationDto>.Fail("This quotation already has a sales order.");

        // Rule: if a (non-cancelled) Proforma was generated for this quotation, the Sales Order
        // must be created from that Proforma (after customer confirmation), not directly.
        var hasActiveProforma = await _proformaRepo.Query().AnyAsync(
            p => p.QuotationId == q.Id
                 && p.Status != ProformaInvoiceStatus.Cancelled
                 && p.Status != ProformaInvoiceStatus.Expired, ct);
        if (hasActiveProforma)
            return ApiResponse<QuotationDto>.Fail(
                "A proforma invoice was generated for this quotation. Create the sales order from that proforma (after customer confirmation) instead.");

        var soCommand = new CreateSalesOrderCommand(
            CustomerId: q.CustomerId,
            OrderDate: DateOnly.FromDateTime(DateTime.UtcNow),
            RequiredDeliveryDate: null,
            CustomerPoRef: q.CustomerReference,
            DeliveryAddress: null,
            Notes: $"Converted from quotation {q.Code}",
            CurrencyId: q.CurrencyId,
            ExchangeRate: q.ExchangeRate,
            Lines: q.Lines.OrderBy(l => l.SortOrder)
                .Select(l => new SalesOrderLineInput(l.ProductId, l.Quantity, l.UnitPrice, l.Description))
                .ToList(),
            Source: Domain.Entities.SalesOrderSource.Quotation);

        var soResult = await _mediator.Send(soCommand, ct);   // creates + saves the Sales Order
        if (!soResult.Success || soResult.Data is null)
            return ApiResponse<QuotationDto>.Fail(soResult.Message ?? "Could not create the sales order.");

        q.Status = QuotationStatus.Converted;
        q.ConvertedSalesOrderId = soResult.Data.Id;
        _repo.Update(q);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetQuotationByIdQuery(q.Id), ct);
    }
}
