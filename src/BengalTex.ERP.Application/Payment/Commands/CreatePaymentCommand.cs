using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Payment.Dtos;
using BengalTex.ERP.Application.Payment.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Payment.Commands;

/// <summary>
/// Creates a Payment against an existing <see cref="SupplierInvoice"/>. Atomic in one
/// SaveChanges:
///   1. Validate invoice is Approved or PartiallyPaid.
///   2. Validate the new payment would not overpay (Σ AmountPaid + Amount ≤ TotalAmount).
///   3. Insert Payment with auto-generated code from "PAY" series.
///   4. Increment <see cref="SupplierInvoice.AmountPaid"/> and recompute status
///      (PartiallyPaid if &lt; Total, Paid if &gt;= Total).
/// </summary>
public sealed record CreatePaymentCommand(
    long SupplierInvoiceId,
    DateOnly PaymentDate,
    decimal Amount,
    string PaymentMethod,                // enum string
    string? ReferenceNumber,
    string? Notes
) : IRequest<ApiResponse<PaymentDto>>;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.SupplierInvoiceId).GreaterThan(0);
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _))
            .WithMessage("Invalid payment method.");
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreatePaymentCommandHandler
    : IRequestHandler<CreatePaymentCommand, ApiResponse<PaymentDto>>
{
    private readonly IRepository<Domain.Entities.Payment, long> _repo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public CreatePaymentCommandHandler(
        IRepository<Domain.Entities.Payment, long> repo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
        _numbering = numbering;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PaymentDto>> Handle(
        CreatePaymentCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _invRepo.GetByIdAsync(cmd.SupplierInvoiceId, cancellationToken);
        if (inv is null) return ApiResponse<PaymentDto>.Fail("Supplier invoice not found.");

        if (inv.Status != Domain.Entities.SupplierInvoiceStatus.Approved &&
            inv.Status != Domain.Entities.SupplierInvoiceStatus.PartiallyPaid)
        {
            return ApiResponse<PaymentDto>.Fail(
                "Payments can only be posted against Approved or partially-paid invoices.");
        }

        var newAmountPaid = inv.AmountPaid + cmd.Amount;
        if (newAmountPaid > inv.TotalAmount)
        {
            var outstanding = inv.TotalAmount - inv.AmountPaid;
            return ApiResponse<PaymentDto>.Fail(
                $"Amount would overpay the invoice (outstanding {outstanding:0.####}, " +
                $"attempted {cmd.Amount:0.####}).");
        }

        var code = await _numbering.NextAsync("PAY", null, cancellationToken);

        var entity = new Domain.Entities.Payment
        {
            Code = code,
            SupplierInvoiceId = cmd.SupplierInvoiceId,
            PaymentDate = cmd.PaymentDate,
            Amount = cmd.Amount,
            PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod),
            ReferenceNumber = string.IsNullOrWhiteSpace(cmd.ReferenceNumber) ? null : cmd.ReferenceNumber.Trim(),
            Notes = cmd.Notes
        };
        await _repo.AddAsync(entity, cancellationToken);

        inv.AmountPaid = newAmountPaid;
        inv.Status = newAmountPaid >= inv.TotalAmount
            ? Domain.Entities.SupplierInvoiceStatus.Paid
            : Domain.Entities.SupplierInvoiceStatus.PartiallyPaid;
        _invRepo.Update(inv);

        await _uow.SaveChangesAsync(cancellationToken);   // persist payment (gets its Id) + invoice

        // Auto-journal: Dr Accounts Payable, Cr Cash/Bank (base BDT via the invoice's rate).
        var cashAccount = entity.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
        var baseAmount = cmd.Amount * inv.ExchangeRate;
        await _journal.PostAsync(
            entity.PaymentDate, $"Payment {entity.Code} against {inv.Code}", "Payment", entity.Id, entity.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.AccountsPayable, baseAmount, 0m),
                new JournalPostingLine(cashAccount, 0m, baseAmount),
            }, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPaymentByIdQuery(entity.Id), cancellationToken);
    }
}
