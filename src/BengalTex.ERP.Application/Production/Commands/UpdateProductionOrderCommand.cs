using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Commands;

public sealed record UpdateProductionOrderCommand(
    long Id,
    int ProductId,
    int BomId,
    decimal Quantity,
    int IssueWarehouseId,
    int ReceiveWarehouseId,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    string? Notes,
    IReadOnlyList<ProductionStageInput>? Stages = null,
    long? SalesOrderId = null,
    long? SalesOrderLineId = null,
    bool RequiresQc = false
) : IRequest<ApiResponse<ProductionOrderDto>>;

public sealed class UpdateProductionOrderCommandValidator : AbstractValidator<UpdateProductionOrderCommand>
{
    public UpdateProductionOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.BomId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.IssueWarehouseId).GreaterThan(0);
        RuleFor(x => x.ReceiveWarehouseId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleForEach(x => x.Stages).ChildRules(stage =>
        {
            stage.RuleFor(s => s.StageName).NotEmpty().MaximumLength(100);
            stage.RuleFor(s => s.ProductionLine).MaximumLength(100);
            stage.RuleFor(s => s.Notes).MaximumLength(1000);
            stage.RuleFor(s => s.PlannedQuantity).GreaterThan(0).When(s => s.PlannedQuantity.HasValue);
        });
    }
}

internal sealed class UpdateProductionOrderCommandHandler
    : IRequestHandler<UpdateProductionOrderCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.Bom> _bomRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IUnitOfWork _uow;
    private readonly IStockReservationService _reservations;
    private readonly IMediator _mediator;

    public UpdateProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.Bom> bomRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IUnitOfWork uow,
        IStockReservationService reservations,
        IMediator mediator)
    {
        _repo = repo;
        _productRepo = productRepo;
        _bomRepo = bomRepo;
        _warehouseRepo = warehouseRepo;
        _soRepo = soRepo;
        _uow = uow;
        _reservations = reservations;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        UpdateProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.Query()
            .Include(p => p.Stages)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");
        if (po.Status != Domain.Entities.ProductionOrderStatus.Draft)
            return ApiResponse<ProductionOrderDto>.Fail("Only draft production orders can be edited.");

        var product = await _productRepo.GetByIdAsync(cmd.ProductId, cancellationToken);
        if (product is null) return ApiResponse<ProductionOrderDto>.Fail("Product not found.");

        var bom = await _bomRepo.GetByIdAsync(cmd.BomId, cancellationToken);
        if (bom is null) return ApiResponse<ProductionOrderDto>.Fail("BOM not found.");
        if (bom.ProductId != cmd.ProductId)
            return ApiResponse<ProductionOrderDto>.Fail("BOM does not belong to the selected product.");

        var issueWh = await _warehouseRepo.GetByIdAsync(cmd.IssueWarehouseId, cancellationToken);
        if (issueWh is null) return ApiResponse<ProductionOrderDto>.Fail("Issue warehouse not found.");

        var receiveWh = await _warehouseRepo.GetByIdAsync(cmd.ReceiveWarehouseId, cancellationToken);
        if (receiveWh is null) return ApiResponse<ProductionOrderDto>.Fail("Receive warehouse not found.");

        // Phase 1 — optional Sales Order link + remaining-quantity guard (excludes this order from the allocated sum).
        var linkError = await ProductionSalesLink.ValidateAsync(
            _soRepo, _repo, cmd.SalesOrderId, cmd.SalesOrderLineId,
            cmd.ProductId, cmd.Quantity, excludeProductionOrderId: po.Id, cancellationToken);
        if (linkError is not null) return ApiResponse<ProductionOrderDto>.Fail(linkError);

        po.SalesOrderId = cmd.SalesOrderId;
        po.SalesOrderLineId = cmd.SalesOrderLineId;
        po.ProductId = cmd.ProductId;
        po.BomId = cmd.BomId;
        po.Quantity = cmd.Quantity;
        po.IssueWarehouseId = cmd.IssueWarehouseId;
        po.ReceiveWarehouseId = cmd.ReceiveWarehouseId;
        po.PlannedStartDate = cmd.PlannedStartDate;
        po.PlannedEndDate = cmd.PlannedEndDate;
        po.RequiresQc = cmd.RequiresQc;
        po.Notes = cmd.Notes;

        // Replace the routing (order is Draft, so no stage has been worked yet).
        po.Stages.Clear();
        if (cmd.Stages is { Count: > 0 })
        {
            var seq = 1;
            foreach (var s in cmd.Stages.OrderBy(s => s.Sequence))
            {
                po.Stages.Add(new Domain.Entities.ProductionStage
                {
                    Sequence = seq++,
                    StageName = s.StageName.Trim(),
                    Status = Domain.Entities.ProductionStageStatus.Pending,
                    PlannedQuantity = s.PlannedQuantity ?? cmd.Quantity,
                    CompletedQuantity = 0m,
                    RejectedQuantity = 0m,
                    ProductionLine = string.IsNullOrWhiteSpace(s.ProductionLine) ? null : s.ProductionLine.Trim(),
                    WorkCenterId = s.WorkCenterId,
                    ShiftId = s.ShiftId,
                    OperatorEmployeeId = s.OperatorEmployeeId,
                    Notes = string.IsNullOrWhiteSpace(s.Notes) ? null : s.Notes.Trim()
                });
            }
        }

        _repo.Update(po);

        // Phase 2 — the BOM / quantity / warehouse may have changed: drop the old reservations and
        // re-reserve fresh against the saved values (two commits keep the snapshot reads clean).
        await _reservations.ReleaseForReferenceAsync("ProductionOrder", po.Id, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _reservations.ReserveForProductionOrderAsync(po.Id, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
