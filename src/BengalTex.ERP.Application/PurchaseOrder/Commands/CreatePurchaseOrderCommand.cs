using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

/// <summary>One raw-material line submitted with a create/update PO request.</summary>
public sealed record PurchaseOrderLineInput(
    int RawMaterialId,
    decimal Quantity,
    decimal UnitPrice,
    string? LineNotes);

public sealed record CreatePurchaseOrderCommand(
    int SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int? DeliveryWarehouseId,
    string? Notes,
    int CurrencyId,
    decimal ExchangeRate,
    IReadOnlyList<PurchaseOrderLineInput> Lines
) : IRequest<ApiResponse<PurchaseOrderDto>>;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.OrderDate).NotEmpty();
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A purchase order must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.RawMaterialId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("The same raw material appears more than once in the PO lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreatePurchaseOrderCommandHandler
    : IRequestHandler<CreatePurchaseOrderCommand, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreatePurchaseOrderCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _warehouseRepo = warehouseRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _currencyRepo = currencyRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        CreatePurchaseOrderCommand cmd, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepo.GetByIdAsync(cmd.SupplierId, cancellationToken);
        if (supplier is null) return ApiResponse<PurchaseOrderDto>.Fail("Supplier not found.");

        var currency = await _currencyRepo.GetByIdAsync(cmd.CurrencyId, cancellationToken);
        if (currency is null) return ApiResponse<PurchaseOrderDto>.Fail("Currency not found.");

        if (cmd.DeliveryWarehouseId.HasValue)
        {
            var warehouse = await _warehouseRepo.GetByIdAsync(cmd.DeliveryWarehouseId.Value, cancellationToken);
            if (warehouse is null)
                return ApiResponse<PurchaseOrderDto>.Fail("Delivery warehouse not found.");
        }

        var rawMaterialIds = cmd.Lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var existingCount = await _rawMaterialRepo.Query()
            .CountAsync(rm => rawMaterialIds.Contains(rm.Id), cancellationToken);
        if (existingCount != rawMaterialIds.Count)
            return ApiResponse<PurchaseOrderDto>.Fail("One or more raw materials not found.");

        var code = await _numbering.NextAsync("PO", null, cancellationToken);

        var entity = new Domain.Entities.PurchaseOrder
        {
            Code = code,
            SupplierId = cmd.SupplierId,
            OrderDate = cmd.OrderDate,
            ExpectedDeliveryDate = cmd.ExpectedDeliveryDate,
            DeliveryWarehouseId = cmd.DeliveryWarehouseId,
            Status = Domain.Entities.PurchaseOrderStatus.Draft,
            CurrencyId = cmd.CurrencyId,
            ExchangeRate = cmd.ExchangeRate,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.PurchaseOrderLine
            {
                RawMaterialId = l.RawMaterialId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                ReceivedQuantity = 0m,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPurchaseOrderByIdQuery(entity.Id), cancellationToken);
    }
}
