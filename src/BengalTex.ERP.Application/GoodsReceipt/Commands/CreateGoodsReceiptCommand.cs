using BengalTex.ERP.Application.GoodsReceipt.Dtos;
using BengalTex.ERP.Application.GoodsReceipt.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.GoodsReceipt.Commands;

/// <summary>One line of a GRN — quantity received against a specific PO line.</summary>
public sealed record GoodsReceiptLineInput(
    long PurchaseOrderLineId,
    decimal ReceivedQuantity,
    string? LineNotes);

public sealed record CreateGoodsReceiptCommand(
    long PurchaseOrderId,
    DateOnly ReceiveDate,
    int ReceivingWarehouseId,
    string? SupplierDeliveryRef,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineInput> Lines
) : IRequest<ApiResponse<GoodsReceiptDto>>;

public sealed class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0);
        RuleFor(x => x.ReceiveDate).NotEmpty();
        RuleFor(x => x.ReceivingWarehouseId).GreaterThan(0);
        RuleFor(x => x.SupplierDeliveryRef).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A goods receipt must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.PurchaseOrderLineId).GreaterThan(0);
            line.RuleFor(l => l.ReceivedQuantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.PurchaseOrderLineId).Distinct().Count() == lines.Count)
            .WithMessage("The same PO line appears more than once — combine the quantities.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateGoodsReceiptCommandHandler
    : IRequestHandler<CreateGoodsReceiptCommand, ApiResponse<GoodsReceiptDto>>
{
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _repo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateGoodsReceiptCommandHandler(
        IRepository<Domain.Entities.GoodsReceiptNote, long> repo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _poRepo = poRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<GoodsReceiptDto>> Handle(
        CreateGoodsReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _poRepo.Query()
            .Include(p => p.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == cmd.PurchaseOrderId, cancellationToken);
        if (po is null) return ApiResponse<GoodsReceiptDto>.Fail("Purchase order not found.");

        if (po.Status != Domain.Entities.PurchaseOrderStatus.Approved &&
            po.Status != Domain.Entities.PurchaseOrderStatus.Sent &&
            po.Status != Domain.Entities.PurchaseOrderStatus.PartiallyReceived)
        {
            return ApiResponse<GoodsReceiptDto>.Fail(
                "Goods can only be received against an Approved, Sent or partially-received purchase order.");
        }

        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.ReceivingWarehouseId, cancellationToken);
        if (warehouse is null)
            return ApiResponse<GoodsReceiptDto>.Fail("Receiving warehouse not found.");

        // Each GRN line must reference a line of this PO, and not over-receive against the remaining qty
        foreach (var line in cmd.Lines)
        {
            var poLine = po.Lines.FirstOrDefault(pl => pl.Id == line.PurchaseOrderLineId);
            if (poLine is null)
                return ApiResponse<GoodsReceiptDto>.Fail(
                    $"PO line {line.PurchaseOrderLineId} does not belong to PO {po.Code}.");

            var remaining = poLine.Quantity - poLine.ReceivedQuantity;
            if (line.ReceivedQuantity > remaining)
                return ApiResponse<GoodsReceiptDto>.Fail(
                    $"PO line {poLine.Id}: would exceed ordered qty ({remaining:0.####} remaining).");
        }

        var code = await _numbering.NextAsync("GRN", null, cancellationToken);

        var entity = new Domain.Entities.GoodsReceiptNote
        {
            Code = code,
            PurchaseOrderId = cmd.PurchaseOrderId,
            ReceiveDate = cmd.ReceiveDate,
            ReceivingWarehouseId = cmd.ReceivingWarehouseId,
            Status = Domain.Entities.GoodsReceiptStatus.Draft,
            SupplierDeliveryRef = cmd.SupplierDeliveryRef,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.GoodsReceiptLine
            {
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ReceivedQuantity = l.ReceivedQuantity,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetGoodsReceiptByIdQuery(entity.Id), cancellationToken);
    }
}
