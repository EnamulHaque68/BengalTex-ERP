using BengalTex.ERP.Application.SalesOrder.Commands;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProformaInvoices.Commands;

/// <summary>
/// Converts an Accepted Proforma (generated from a quotation) into a draft Sales Order after the
/// customer confirms the order. The confirmation (how + reference + date + attachment) is recorded
/// on the proforma for audit. Rule: only one active Sales Order per source quotation.
/// </summary>
public sealed record ConvertProformaToSalesOrderCommand(
    long ProformaInvoiceId,
    string CustomerConfirmationType,        // PurchaseOrder | LetterOfCredit | AdvancePayment | SignedProforma | EmailApproval
    string? CustomerConfirmationReference,
    DateOnly? CustomerConfirmationDate,
    string? CustomerConfirmationAttachment  // storage path (already uploaded), optional
) : IRequest<ApiResponse<long>>;

public sealed class ConvertProformaToSalesOrderCommandValidator : AbstractValidator<ConvertProformaToSalesOrderCommand>
{
    public static readonly string[] AllowedTypes =
        { "PurchaseOrder", "LetterOfCredit", "AdvancePayment", "SignedProforma", "EmailApproval" };

    public ConvertProformaToSalesOrderCommandValidator()
    {
        RuleFor(x => x.ProformaInvoiceId).GreaterThan(0);
        RuleFor(x => x.CustomerConfirmationType).NotEmpty()
            .Must(t => AllowedTypes.Contains(t))
            .WithMessage("Confirmation type must be one of: PurchaseOrder, LetterOfCredit, AdvancePayment, SignedProforma, EmailApproval.");
        RuleFor(x => x.CustomerConfirmationReference).MaximumLength(200);
    }
}

internal sealed class ConvertProformaToSalesOrderCommandHandler
    : IRequestHandler<ConvertProformaToSalesOrderCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.Quotation, long> _quotationRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ConvertProformaToSalesOrderCommandHandler(
        IRepository<Domain.Entities.ProformaInvoice, long> repo,
        IRepository<Domain.Entities.Quotation, long> quotationRepo,
        IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _quotationRepo = quotationRepo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<long>> Handle(ConvertProformaToSalesOrderCommand cmd, CancellationToken ct)
    {
        var pf = await _repo.Query().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == cmd.ProformaInvoiceId, ct);
        if (pf is null) return ApiResponse<long>.Fail("Proforma invoice not found.");
        if (pf.Status != ProformaInvoiceStatus.Accepted)
            return ApiResponse<long>.Fail($"Only an Accepted proforma can be converted to a sales order (current: {pf.Status}).");
        if (pf.ConvertedSalesOrderId.HasValue || pf.ConvertedCustomerInvoiceId.HasValue)
            return ApiResponse<long>.Fail("This proforma has already been converted.");

        // Rule: one active Sales Order per source quotation.
        Domain.Entities.Quotation? quotation = null;
        if (pf.QuotationId is long qid)
        {
            quotation = await _quotationRepo.Query().FirstOrDefaultAsync(q => q.Id == qid, ct);
            if (quotation?.ConvertedSalesOrderId is not null)
                return ApiResponse<long>.Fail("The source quotation already has a sales order.");
        }

        var soResult = await _mediator.Send(new CreateSalesOrderCommand(
            CustomerId: pf.CustomerId,
            OrderDate: DateOnly.FromDateTime(DateTime.UtcNow),
            RequiredDeliveryDate: null,
            CustomerPoRef: cmd.CustomerConfirmationType == "PurchaseOrder" ? cmd.CustomerConfirmationReference : null,
            DeliveryAddress: null,
            Notes: $"Converted from proforma {pf.Code}" + (quotation is not null ? $" (quotation {quotation.Code})" : ""),
            CurrencyId: pf.CurrencyId,
            ExchangeRate: pf.ExchangeRate,
            Lines: pf.Lines.OrderBy(l => l.SortOrder)
                .Select(l => new SalesOrderLineInput(l.ProductId, l.Quantity, l.UnitPrice, l.LineNotes))
                .ToList(),
            Source: Domain.Entities.SalesOrderSource.ProformaInvoice), ct);

        if (!soResult.Success || soResult.Data is null)
            return ApiResponse<long>.Fail(soResult.Message ?? "Could not create the sales order.");

        // Record confirmation + conversion on the proforma
        pf.Status = ProformaInvoiceStatus.Converted;
        pf.ConvertedSalesOrderId = soResult.Data.Id;
        pf.CustomerConfirmationType = cmd.CustomerConfirmationType;
        pf.CustomerConfirmationReference = string.IsNullOrWhiteSpace(cmd.CustomerConfirmationReference) ? null : cmd.CustomerConfirmationReference.Trim();
        pf.CustomerConfirmationDate = cmd.CustomerConfirmationDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(cmd.CustomerConfirmationAttachment))
            pf.CustomerConfirmationAttachment = cmd.CustomerConfirmationAttachment;
        _repo.Update(pf);

        // Close the source quotation
        if (quotation is not null)
        {
            quotation.Status = QuotationStatus.Converted;
            quotation.ConvertedSalesOrderId = soResult.Data.Id;
            _quotationRepo.Update(quotation);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(soResult.Data.Id, $"Proforma {pf.Code} converted to sales order {soResult.Data.Code}.");
    }
}

/// <summary>Returns the storage path of a proforma's customer-confirmation attachment (null if none), for serving.</summary>
public sealed record GetProformaConfirmationAttachmentPathQuery(long ProformaInvoiceId) : IRequest<string?>;

internal sealed class GetProformaConfirmationAttachmentPathQueryHandler
    : IRequestHandler<GetProformaConfirmationAttachmentPathQuery, string?>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    public GetProformaConfirmationAttachmentPathQueryHandler(IRepository<Domain.Entities.ProformaInvoice, long> repo) => _repo = repo;

    public async Task<string?> Handle(GetProformaConfirmationAttachmentPathQuery req, CancellationToken ct)
        => (await _repo.Query().AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProformaInvoiceId, ct))?.CustomerConfirmationAttachment;
}
