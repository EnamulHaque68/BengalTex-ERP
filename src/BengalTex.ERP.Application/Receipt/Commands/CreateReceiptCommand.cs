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
/// Creates a Receipt against an existing <see cref="CustomerInvoice"/>. Atomic in one
/// SaveChanges:
///   1. Validate invoice is Issued or PartiallyPaid.
///   2. Validate the new payment would not overpay (Σ AmountPaid + Amount ≤ TotalAmount).
///   3. Insert Receipt with auto-generated code from "RCT" series.
///   4. Increment <see cref="CustomerInvoice.AmountPaid"/> and recompute status
///      (PartiallyPaid if &lt; Total, Paid if &gt;= Total).
/// </summary>
public sealed record CreateReceiptCommand(
    long CustomerInvoiceId,
    DateOnly ReceiptDate,
    decimal Amount,
    string PaymentMethod,                // enum string
    string? ReferenceNumber,
    string? Notes
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
                "Receipts can only be posted against Issued or partially-paid invoices.");
        }

        var newAmountPaid = inv.AmountPaid + cmd.Amount;
        if (newAmountPaid > inv.TotalAmount)
        {
            var outstanding = inv.TotalAmount - inv.AmountPaid;
            return ApiResponse<ReceiptDto>.Fail(
                $"Amount would overpay the invoice (outstanding {outstanding:0.####}, " +
                $"attempted {cmd.Amount:0.####}).");
        }

        var code = await _numbering.NextAsync("RCT", null, cancellationToken);

        var entity = new Domain.Entities.Receipt
        {
            Code = code,
            CustomerInvoiceId = cmd.CustomerInvoiceId,
            ReceiptDate = cmd.ReceiptDate,
            Amount = cmd.Amount,
            PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod),
            ReferenceNumber = string.IsNullOrWhiteSpace(cmd.ReferenceNumber) ? null : cmd.ReferenceNumber.Trim(),
            Notes = cmd.Notes
        };
        await _repo.AddAsync(entity, cancellationToken);

        inv.AmountPaid = newAmountPaid;
        inv.Status = newAmountPaid >= inv.TotalAmount
            ? Domain.Entities.CustomerInvoiceStatus.Paid
            : Domain.Entities.CustomerInvoiceStatus.PartiallyPaid;
        _invRepo.Update(inv);

        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetReceiptByIdQuery(entity.Id), cancellationToken);
    }
}
