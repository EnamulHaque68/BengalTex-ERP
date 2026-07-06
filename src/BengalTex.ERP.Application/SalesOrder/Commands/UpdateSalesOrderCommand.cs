using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

public sealed record UpdateSalesOrderCommand(
    long Id,
    int CustomerId,
    DateOnly OrderDate,
    DateOnly? RequiredDeliveryDate,
    string? CustomerPoRef,
    string? DeliveryAddress,
    string? Notes,
    int CurrencyId,
    decimal ExchangeRate,
    IReadOnlyList<SalesOrderLineInput> Lines
) : IRequest<ApiResponse<SalesOrderDto>>;

public sealed class UpdateSalesOrderCommandValidator : AbstractValidator<UpdateSalesOrderCommand>
{
    public UpdateSalesOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateSalesOrderCommandHandler
    : IRequestHandler<UpdateSalesOrderCommand, ApiResponse<SalesOrderDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _currencyRepo = currencyRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SalesOrderDto>> Handle(
        UpdateSalesOrderCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);

        if (so is null) return ApiResponse<SalesOrderDto>.Fail("Sales order not found.");
        if (so.Status != Domain.Entities.SalesOrderStatus.Draft)
            return ApiResponse<SalesOrderDto>.Fail("Only draft sales orders can be edited.");

        var customer = await _customerRepo.GetByIdAsync(cmd.CustomerId, cancellationToken);
        if (customer is null) return ApiResponse<SalesOrderDto>.Fail("Customer not found.");

        var currency = await _currencyRepo.GetByIdAsync(cmd.CurrencyId, cancellationToken);
        if (currency is null) return ApiResponse<SalesOrderDto>.Fail("Currency not found.");

        var productIds = cmd.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingCount = await _productRepo.Query()
            .CountAsync(p => productIds.Contains(p.Id), cancellationToken);
        if (existingCount != productIds.Count)
            return ApiResponse<SalesOrderDto>.Fail("One or more products not found.");

        so.CustomerId = cmd.CustomerId;
        so.OrderDate = cmd.OrderDate;
        so.RequiredDeliveryDate = cmd.RequiredDeliveryDate;
        so.CustomerPoRef = string.IsNullOrWhiteSpace(cmd.CustomerPoRef) ? null : cmd.CustomerPoRef.Trim();
        so.DeliveryAddress = string.IsNullOrWhiteSpace(cmd.DeliveryAddress) ? null : cmd.DeliveryAddress.Trim();
        so.CurrencyId = cmd.CurrencyId;
        so.ExchangeRate = cmd.ExchangeRate;
        so.Notes = cmd.Notes;

        so.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            so.Lines.Add(new Domain.Entities.SalesOrderLine
            {
                ProductId = line.ProductId,
                StyleId = line.StyleId,   // Phase A3
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSalesOrderByIdQuery(so.Id), cancellationToken);
    }
}
