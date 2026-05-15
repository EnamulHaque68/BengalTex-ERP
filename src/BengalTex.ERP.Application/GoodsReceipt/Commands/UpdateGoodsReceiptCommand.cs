using BengalTex.ERP.Application.GoodsReceipt.Dtos;
using BengalTex.ERP.Application.GoodsReceipt.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.GoodsReceipt.Commands;

public sealed record UpdateGoodsReceiptCommand(
    long Id,
    DateOnly ReceiveDate,
    int ReceivingWarehouseId,
    string? SupplierDeliveryRef,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineInput> Lines
) : IRequest<ApiResponse<GoodsReceiptDto>>;

public sealed class UpdateGoodsReceiptCommandValidator : AbstractValidator<UpdateGoodsReceiptCommand>
{
    public UpdateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateGoodsReceiptCommandHandler
    : IRequestHandler<UpdateGoodsReceiptCommand, ApiResponse<GoodsReceiptDto>>
{
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _repo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateGoodsReceiptCommandHandler(
        IRepository<Domain.Entities.GoodsReceiptNote, long> repo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _poRepo = poRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<GoodsReceiptDto>> Handle(
        UpdateGoodsReceiptCommand cmd, CancellationToken cancellationToken)
    {
        var grn = await _repo.Query()
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == cmd.Id, cancellationToken);

        if (grn is null) return ApiResponse<GoodsReceiptDto>.Fail("Goods receipt not found.");
        if (grn.Status != Domain.Entities.GoodsReceiptStatus.Draft)
            return ApiResponse<GoodsReceiptDto>.Fail("Only draft goods receipts can be edited.");

        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.ReceivingWarehouseId, cancellationToken);
        if (warehouse is null)
            return ApiResponse<GoodsReceiptDto>.Fail("Receiving warehouse not found.");

        var po = await _poRepo.Query()
            .Include(p => p.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == grn.PurchaseOrderId, cancellationToken);
        if (po is null) return ApiResponse<GoodsReceiptDto>.Fail("Parent purchase order not found.");

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

        grn.ReceiveDate = cmd.ReceiveDate;
        grn.ReceivingWarehouseId = cmd.ReceivingWarehouseId;
        grn.SupplierDeliveryRef = cmd.SupplierDeliveryRef;
        grn.Notes = cmd.Notes;

        grn.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            grn.Lines.Add(new Domain.Entities.GoodsReceiptLine
            {
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                ReceivedQuantity = line.ReceivedQuantity,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(grn);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetGoodsReceiptByIdQuery(grn.Id), cancellationToken);
    }
}
