using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

public sealed record UpdatePurchaseOrderCommand(
    long Id,
    int SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int? DeliveryWarehouseId,
    string? Notes,
    int CurrencyId,
    decimal ExchangeRate,
    IReadOnlyList<PurchaseOrderLineInput> Lines
) : IRequest<ApiResponse<PurchaseOrderDto>>;

public sealed class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdatePurchaseOrderCommandHandler
    : IRequestHandler<UpdatePurchaseOrderCommand, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdatePurchaseOrderCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _warehouseRepo = warehouseRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _currencyRepo = currencyRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        UpdatePurchaseOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, cancellationToken);

        if (po is null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");
        if (po.Status != Domain.Entities.PurchaseOrderStatus.Draft)
            return ApiResponse<PurchaseOrderDto>.Fail("Only draft purchase orders can be edited.");

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

        po.SupplierId = cmd.SupplierId;
        po.OrderDate = cmd.OrderDate;
        po.ExpectedDeliveryDate = cmd.ExpectedDeliveryDate;
        po.DeliveryWarehouseId = cmd.DeliveryWarehouseId;
        po.CurrencyId = cmd.CurrencyId;
        po.ExchangeRate = cmd.ExchangeRate;
        po.Notes = cmd.Notes;

        // Draft lines carry no history — replace the whole set
        po.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            po.Lines.Add(new Domain.Entities.PurchaseOrderLine
            {
                RawMaterialId = line.RawMaterialId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                ReceivedQuantity = 0m,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), cancellationToken);
    }
}
