using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

/// <summary>One product line submitted with a create/update SO request.</summary>
public sealed record SalesOrderLineInput(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    string? LineNotes);

public sealed record CreateSalesOrderCommand(
    int CustomerId,
    DateOnly OrderDate,
    DateOnly? RequiredDeliveryDate,
    string? CustomerPoRef,
    string? DeliveryAddress,
    string? Notes,
    int CurrencyId,
    decimal ExchangeRate,
    IReadOnlyList<SalesOrderLineInput> Lines,
    Domain.Entities.SalesOrderSource? Source = null   // traceability: Quotation | ProformaInvoice (null = direct)
) : IRequest<ApiResponse<SalesOrderDto>>;

public sealed class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.OrderDate).NotEmpty();
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.CustomerPoRef).MaximumLength(100);
        RuleFor(x => x.DeliveryAddress).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A sales order must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("The same product appears more than once in the SO lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateSalesOrderCommandHandler
    : IRequestHandler<CreateSalesOrderCommand, ApiResponse<SalesOrderDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _currencyRepo = currencyRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SalesOrderDto>> Handle(
        CreateSalesOrderCommand cmd, CancellationToken cancellationToken)
    {
        var customer = await _customerRepo.GetByIdAsync(cmd.CustomerId, cancellationToken);
        if (customer is null) return ApiResponse<SalesOrderDto>.Fail("Customer not found.");

        var currency = await _currencyRepo.GetByIdAsync(cmd.CurrencyId, cancellationToken);
        if (currency is null) return ApiResponse<SalesOrderDto>.Fail("Currency not found.");

        var productIds = cmd.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingCount = await _productRepo.Query()
            .CountAsync(p => productIds.Contains(p.Id), cancellationToken);
        if (existingCount != productIds.Count)
            return ApiResponse<SalesOrderDto>.Fail("One or more products not found.");

        var code = await _numbering.NextAsync("SO", null, cancellationToken);

        var entity = new Domain.Entities.SalesOrder
        {
            Code = code,
            CustomerId = cmd.CustomerId,
            OrderDate = cmd.OrderDate,
            RequiredDeliveryDate = cmd.RequiredDeliveryDate,
            CustomerPoRef = string.IsNullOrWhiteSpace(cmd.CustomerPoRef) ? null : cmd.CustomerPoRef.Trim(),
            DeliveryAddress = string.IsNullOrWhiteSpace(cmd.DeliveryAddress) ? null : cmd.DeliveryAddress.Trim(),
            Status = Domain.Entities.SalesOrderStatus.Draft,
            Source = cmd.Source,
            CurrencyId = cmd.CurrencyId,
            ExchangeRate = cmd.ExchangeRate,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.SalesOrderLine
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

        return await _mediator.Send(new GetSalesOrderByIdQuery(entity.Id), cancellationToken);
    }
}
