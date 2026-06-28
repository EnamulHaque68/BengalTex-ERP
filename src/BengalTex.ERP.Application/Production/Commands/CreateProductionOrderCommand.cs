using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>Optional routing stage on a production order (Cutting / Sewing / …).</summary>
public sealed record ProductionStageInput(
    int Sequence,
    string StageName,
    decimal? PlannedQuantity,            // null → defaults to the order quantity
    string? ProductionLine,
    int? OperatorEmployeeId,
    string? Notes,
    int? WorkCenterId = null,            // Phase 4 — capacity planning
    int? ShiftId = null);               // Phase 4 — multi-shift

public sealed record CreateProductionOrderCommand(
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

public sealed class CreateProductionOrderCommandValidator : AbstractValidator<CreateProductionOrderCommand>
{
    public CreateProductionOrderCommandValidator()
    {
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

internal sealed class CreateProductionOrderCommandHandler
    : IRequestHandler<CreateProductionOrderCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.Bom> _bomRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IStockReservationService _reservations;
    private readonly IMediator _mediator;

    public CreateProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.Bom> bomRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IStockReservationService reservations,
        IMediator mediator)
    {
        _repo = repo;
        _productRepo = productRepo;
        _bomRepo = bomRepo;
        _warehouseRepo = warehouseRepo;
        _soRepo = soRepo;
        _uow = uow;
        _numbering = numbering;
        _reservations = reservations;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        CreateProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
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

        // Phase 1 — optional Sales Order link + remaining-quantity guard (standalone runs skip this).
        var linkError = await ProductionSalesLink.ValidateAsync(
            _soRepo, _repo, cmd.SalesOrderId, cmd.SalesOrderLineId,
            cmd.ProductId, cmd.Quantity, excludeProductionOrderId: null, cancellationToken);
        if (linkError is not null) return ApiResponse<ProductionOrderDto>.Fail(linkError);

        var code = await _numbering.NextAsync("PRD", null, cancellationToken);

        var entity = new Domain.Entities.ProductionOrder
        {
            Code = code,
            SalesOrderId = cmd.SalesOrderId,
            SalesOrderLineId = cmd.SalesOrderLineId,
            ProductId = cmd.ProductId,
            BomId = cmd.BomId,
            Quantity = cmd.Quantity,
            IssueWarehouseId = cmd.IssueWarehouseId,
            ReceiveWarehouseId = cmd.ReceiveWarehouseId,
            PlannedStartDate = cmd.PlannedStartDate,
            PlannedEndDate = cmd.PlannedEndDate,
            Status = Domain.Entities.ProductionOrderStatus.Draft,
            RequiresQc = cmd.RequiresQc,
            Notes = cmd.Notes
        };

        if (cmd.Stages is { Count: > 0 })
        {
            var seq = 1;
            foreach (var s in cmd.Stages.OrderBy(s => s.Sequence))
            {
                entity.Stages.Add(new Domain.Entities.ProductionStage
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

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Phase 2 — soft-reserve this run's BOM raw materials in the issue warehouse.
        await _reservations.ReserveForProductionOrderAsync(entity.Id, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(entity.Id), cancellationToken);
    }
}
