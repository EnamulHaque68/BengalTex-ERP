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
    string? LineNotes);

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
    IReadOnlyList<CustomerInvoiceLineInput> Lines
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
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("The same product appears more than once in the invoice lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateCustomerInvoiceCommandHandler
    : IRequestHandler<CreateCustomerInvoiceCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _soRepo = soRepo;
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
            VatRate = cmd.VatRate,
            SubtotalAmount = subtotal,
            VatAmount = vatAmount,
            TotalAmount = total,
            AmountPaid = 0m,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.CustomerInvoiceLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(entity.Id), cancellationToken);
    }
}
