using BengalTex.ERP.Application.Receipt.Dtos;
using BengalTex.ERP.Application.Receipt.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Receipt.Commands;

/// <summary>
/// Creates a <b>Draft</b> Receipt against an existing <see cref="CustomerInvoice"/>. A draft does
/// NOT affect the invoice — it neither changes <c>AmountPaid</c>/status nor posts any journal.
/// Those happen only when the receipt is later <b>Posted</b> (see <c>PostReceiptCommand</c>).
///   1. Validate invoice is Issued or PartiallyPaid.
///   2. Soft over-payment guard against already-posted receipts (the authoritative check is at Post).
///   3. Insert a Draft Receipt with an auto-generated code from the "RCT" series.
/// </summary>
public sealed record CreateReceiptCommand(
    long CustomerInvoiceId,
    DateOnly ReceiptDate,
    decimal Amount,
    string PaymentMethod,                // enum string
    string? ReferenceNumber,
    string? Notes,
    decimal? ExchangeRate = null,        // BDT/currency at receipt time; null → invoice's rate (no FX)
    decimal BankChargeAmount = 0m,       // Phase A6b — FDBP bank commission (BDT → 5600)
    decimal InterestAmount = 0m          // Phase A6b — FDBP interest/discount (BDT → 5860)
) : IRequest<ApiResponse<ReceiptDto>>;

public sealed class CreateReceiptCommandValidator : AbstractValidator<CreateReceiptCommand>
{
    public CreateReceiptCommandValidator()
    {
        RuleFor(x => x.CustomerInvoiceId).GreaterThan(0);
        RuleFor(x => x.ReceiptDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _))
            .WithMessage("Invalid payment method.");
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.ExchangeRate).GreaterThan(0).When(x => x.ExchangeRate.HasValue);
        RuleFor(x => x.BankChargeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InterestAmount).GreaterThanOrEqualTo(0);
    }
}

internal sealed class CreateReceiptCommandHandler
    : IRequestHandler<CreateReceiptCommand, ApiResponse<ReceiptDto>>
{
    private readonly IRepository<Domain.Entities.Receipt, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateReceiptCommandHandler(
        IRepository<Domain.Entities.Receipt, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ReceiptDto>> Handle(
        CreateReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _invRepo.GetByIdAsync(cmd.CustomerInvoiceId, cancellationToken);
        if (inv is null) return ApiResponse<ReceiptDto>.Fail("Customer invoice not found.");

        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Issued &&
            inv.Status != Domain.Entities.CustomerInvoiceStatus.PartiallyPaid)
        {
            return ApiResponse<ReceiptDto>.Fail(
                "Receipts can only be recorded against Issued or partially-paid invoices.");
        }

        // Soft guard — AmountPaid reflects only POSTED receipts. The hard, authoritative
        // over-payment check happens when the draft is posted.
        if (inv.AmountPaid + cmd.Amount > inv.TotalAmount)
        {
            var outstanding = inv.TotalAmount - inv.AmountPaid;
            return ApiResponse<ReceiptDto>.Fail(
                $"Amount would overpay the invoice (outstanding {outstanding:0.####}, " +
                $"attempted {cmd.Amount:0.####}).");
        }

        var code = await _numbering.NextAsync("RCT", null, cancellationToken);

        // Receipt-time rate: caller-supplied, else the invoice's locked rate (= no FX effect).
        var receiptRate = cmd.ExchangeRate ?? inv.ExchangeRate;

        var entity = new Domain.Entities.Receipt
        {
            Code = code,
            CustomerInvoiceId = cmd.CustomerInvoiceId,
            ReceiptDate = cmd.ReceiptDate,
            Amount = cmd.Amount,
            ExchangeRate = receiptRate,
            PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod),
            Status = Domain.Entities.ReceiptStatus.Draft,   // draft — invoice untouched until Post
            ReferenceNumber = string.IsNullOrWhiteSpace(cmd.ReferenceNumber) ? null : cmd.ReferenceNumber.Trim(),
            BankChargeAmount = Math.Round(cmd.BankChargeAmount, 2, MidpointRounding.AwayFromZero),
            InterestAmount = Math.Round(cmd.InterestAmount, 2, MidpointRounding.AwayFromZero),
            Notes = cmd.Notes
        };
        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetReceiptByIdQuery(entity.Id), cancellationToken);
    }
}
