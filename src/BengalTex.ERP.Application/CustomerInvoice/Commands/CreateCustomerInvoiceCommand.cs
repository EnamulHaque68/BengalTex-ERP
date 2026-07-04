using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>One product line submitted with a create/update Customer Invoice request.</summary>
public sealed record CustomerInvoiceLineInput(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    string? LineNotes,
    long? SalesOrderLineId = null);   // links the line to its originating SO line (drives invoice coverage)

/// <summary>
/// Creates a Draft Customer Invoice derived from an existing Sales Order. The
/// frontend typically pre-populates <see cref="Lines"/> from the SO lines, but the
/// command itself doesn't enforce that the products match the SO (allows partial
/// invoicing + miscellaneous-charge lines). If <see cref="DueDate"/> is null, it's
/// defaulted to <c>InvoiceDate + Customer.PaymentTermsDays</c>.
/// </summary>
public sealed record CreateCustomerInvoiceCommand(
    long SalesOrderId,
    decimal VatRate,                 // 0.0 to 1.0 (e.g. 0.15 for Bangladesh 15% standard)
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string? Notes,
    IReadOnlyList<CustomerInvoiceLineInput> Lines,
    bool IsOpening = false           // Phase A1 — go-live opening invoice (no GL / challan on issue)
) : IRequest<ApiResponse<CustomerInvoiceDto>>;

public sealed class CreateCustomerInvoiceCommandValidator : AbstractValidator<CreateCustomerInvoiceCommand>
{
    public CreateCustomerInvoiceCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).GreaterThan(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m)
            .WithMessage("VAT rate must be between 0 (exempt) and 1 (100%).");
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A customer invoice must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        // A given SO line may be billed by at most one line on a single invoice (the same product
        // may still appear twice if it comes from two different SO lines, or as an ad-hoc line).
        RuleFor(x => x.Lines)
            .Must(lines =>
            {
                var soLineIds = lines.Where(l => l.SalesOrderLineId.HasValue)
                                     .Select(l => l.SalesOrderLineId!.Value).ToList();
                return soLineIds.Distinct().Count() == soLineIds.Count;
            })
            .WithMessage("The same sales-order line appears more than once in the invoice lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateCustomerInvoiceCommandHandler
    : IRequestHandler<CreateCustomerInvoiceCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IRepository<Domain.Entities.SalesOrderLine, long> _soLineRepo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.SalesOrderLine, long> soLineRepo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _soRepo = soRepo;
        _soLineRepo = soLineRepo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        CreateCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _soRepo.GetByIdAsync(cmd.SalesOrderId, cancellationToken);
        if (so is null) return ApiResponse<CustomerInvoiceDto>.Fail("Sales order not found.");

        if (so.Status != Domain.Entities.SalesOrderStatus.Confirmed &&
            so.Status != Domain.Entities.SalesOrderStatus.PartiallyDispatched &&
            so.Status != Domain.Entities.SalesOrderStatus.Dispatched &&
            so.Status != Domain.Entities.SalesOrderStatus.Delivered)
        {
            return ApiResponse<CustomerInvoiceDto>.Fail(
                "Customer invoice can only be raised against a Confirmed (or further) sales order.");
        }

        var customer = await _customerRepo.GetByIdAsync(so.CustomerId, cancellationToken);
        if (customer is null) return ApiResponse<CustomerInvoiceDto>.Fail("Customer not found.");

        var productIds = cmd.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingCount = await _productRepo.Query()
            .CountAsync(p => productIds.Contains(p.Id), cancellationToken);
        if (existingCount != productIds.Count)
            return ApiResponse<CustomerInvoiceDto>.Fail("One or more products not found.");

        // ── Invoice-coverage guard (full/partial tracking) ──
        // Each line that links to an SO line may only bill its remaining (Quantity − InvoicedQuantity).
        // Load + validate, then consume; all SO-line writes commit atomically with the invoice below.
        var linkedLines = cmd.Lines.Where(l => l.SalesOrderLineId.HasValue).ToList();
        var soLineMap = new Dictionary<long, Domain.Entities.SalesOrderLine>();
        foreach (var soLineId in linkedLines.Select(l => l.SalesOrderLineId!.Value).Distinct())
        {
            var soLine = await _soLineRepo.GetByIdAsync(soLineId, cancellationToken);
            if (soLine is null || soLine.SalesOrderId != cmd.SalesOrderId)
                return ApiResponse<CustomerInvoiceDto>.Fail(
                    $"Sales-order line {soLineId} does not belong to this sales order.");
            soLineMap[soLineId] = soLine;
        }
        foreach (var grp in linkedLines.GroupBy(l => l.SalesOrderLineId!.Value))
        {
            var soLine = soLineMap[grp.Key];
            var requested = grp.Sum(x => x.Quantity);
            var remaining = soLine.Quantity - soLine.InvoicedQuantity;
            if (requested > remaining)
                return ApiResponse<CustomerInvoiceDto>.Fail(
                    $"Cannot invoice {requested:0.####} — only {remaining:0.####} remaining to invoice on this order line.");
        }

        var code = await _numbering.NextAsync("INV", null, cancellationToken);

        var dueDate = cmd.DueDate ?? cmd.InvoiceDate.AddDays(customer.CreditPeriodDays);
        var subtotal = cmd.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var vatAmount = Math.Round(subtotal * cmd.VatRate, 4, MidpointRounding.AwayFromZero);
        var total = subtotal + vatAmount;

        var entity = new Domain.Entities.CustomerInvoice
        {
            Code = code,
            CustomerId = so.CustomerId,
            SalesOrderId = cmd.SalesOrderId,
            InvoiceDate = cmd.InvoiceDate,
            DueDate = dueDate,
            Status = Domain.Entities.CustomerInvoiceStatus.Draft,
            CurrencyId = so.CurrencyId,          // invoice inherits the SO's currency
            ExchangeRate = so.ExchangeRate,
            VatRate = cmd.VatRate,
            SubtotalAmount = subtotal,
            VatAmount = vatAmount,
            TotalAmount = total,
            AmountPaid = 0m,
            IsOpening = cmd.IsOpening,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.CustomerInvoiceLine
            {
                ProductId = l.ProductId,
                SalesOrderLineId = l.SalesOrderLineId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        // Consume the SO-line coverage (tracked SO lines persist with the SaveChanges below).
        foreach (var grp in linkedLines.GroupBy(l => l.SalesOrderLineId!.Value))
        {
            var soLine = soLineMap[grp.Key];
            soLine.InvoicedQuantity += grp.Sum(x => x.Quantity);
            _soLineRepo.Update(soLine);
        }

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(entity.Id), cancellationToken);
    }
}
