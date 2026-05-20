using BengalTex.ERP.Application.QcInspection.Dtos;
using BengalTex.ERP.Application.QcInspection.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QcInspection.Commands;

/// <summary>One inspected line: an RM (incoming) or Product (FG) with inspected + passed qty.</summary>
public sealed record QcInspectionLineInput(
    int? RawMaterialId,
    int? ProductId,
    decimal InspectedQuantity,
    decimal PassedQuantity,
    string? DefectNotes);

/// <summary>
/// Creates a Draft QC inspection against a posted GRN (IncomingMaterial) or completed
/// Production Order (FinishedGoods). No stock movement yet — Posting routes rejected qty
/// to quarantine. The inspected-from warehouse is derived from the source document.
/// </summary>
public sealed record CreateQcInspectionCommand(
    string SourceType,                  // "IncomingMaterial" | "FinishedGoods"
    long? GoodsReceiptNoteId,
    long? ProductionOrderId,
    DateOnly InspectionDate,
    int QuarantineWarehouseId,
    string? InspectedBy,
    string? Notes,
    IReadOnlyList<QcInspectionLineInput> Lines
) : IRequest<ApiResponse<QcInspectionDto>>;

public sealed class CreateQcInspectionCommandValidator : AbstractValidator<CreateQcInspectionCommand>
{
    public CreateQcInspectionCommandValidator()
    {
        RuleFor(x => x.SourceType).NotEmpty()
            .Must(s => s is "IncomingMaterial" or "FinishedGoods")
            .WithMessage("SourceType must be IncomingMaterial or FinishedGoods.");
        RuleFor(x => x.QuarantineWarehouseId).GreaterThan(0);
        RuleFor(x => x.InspectionDate).NotEmpty();
        RuleFor(x => x.InspectedBy).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.GoodsReceiptNoteId).GreaterThan(0)
            .When(x => x.SourceType == "IncomingMaterial")
            .WithMessage("GoodsReceiptNoteId is required for an incoming-material inspection.");
        RuleFor(x => x.ProductionOrderId).GreaterThan(0)
            .When(x => x.SourceType == "FinishedGoods")
            .WithMessage("ProductionOrderId is required for a finished-goods inspection.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A QC inspection must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.InspectedQuantity).GreaterThan(0);
            line.RuleFor(l => l.PassedQuantity).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.DefectNotes).MaximumLength(1000);
            line.RuleFor(l => l)
                .Must(l => l.PassedQuantity <= l.InspectedQuantity)
                .WithMessage("Passed quantity cannot exceed inspected quantity.");
            line.RuleFor(l => l)
                .Must(l => (l.RawMaterialId.HasValue && !l.ProductId.HasValue)
                        || (!l.RawMaterialId.HasValue && l.ProductId.HasValue))
                .WithMessage("Each line must have exactly one of RawMaterialId or ProductId.");
        });
    }
}

internal sealed class CreateQcInspectionCommandHandler
    : IRequestHandler<CreateQcInspectionCommand, ApiResponse<QcInspectionDto>>
{
    private readonly IRepository<Domain.Entities.QcInspection, long> _repo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _prodRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateQcInspectionCommandHandler(
        IRepository<Domain.Entities.QcInspection, long> repo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.ProductionOrder, long> prodRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _grnRepo = grnRepo;
        _prodRepo = prodRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QcInspectionDto>> Handle(
        CreateQcInspectionCommand cmd, CancellationToken cancellationToken)
    {
        var sourceType = Enum.Parse<Domain.Entities.QcSourceType>(cmd.SourceType);

        var quarantine = await _warehouseRepo.GetByIdAsync(cmd.QuarantineWarehouseId, cancellationToken);
        if (quarantine is null) return ApiResponse<QcInspectionDto>.Fail("Quarantine warehouse not found.");

        int inspectedFromWarehouseId;

        if (sourceType == Domain.Entities.QcSourceType.IncomingMaterial)
        {
            var grn = await _grnRepo.Query()
                .Include(g => g.Lines).ThenInclude(l => l.PurchaseOrderLine)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == cmd.GoodsReceiptNoteId, cancellationToken);
            if (grn is null) return ApiResponse<QcInspectionDto>.Fail("Goods receipt note not found.");
            if (grn.Status != Domain.Entities.GoodsReceiptStatus.Posted)
                return ApiResponse<QcInspectionDto>.Fail("QC can only inspect a Posted goods receipt note.");

            var grnRmIds = grn.Lines.Select(l => l.PurchaseOrderLine.RawMaterialId).ToHashSet();
            foreach (var line in cmd.Lines)
            {
                if (!line.RawMaterialId.HasValue)
                    return ApiResponse<QcInspectionDto>.Fail("Incoming-material inspection lines must reference a raw material.");
                if (!grnRmIds.Contains(line.RawMaterialId.Value))
                    return ApiResponse<QcInspectionDto>.Fail(
                        $"Raw material {line.RawMaterialId} was not received on GRN {grn.Code}.");
            }

            inspectedFromWarehouseId = grn.ReceivingWarehouseId;
        }
        else
        {
            var prod = await _prodRepo.GetByIdAsync(cmd.ProductionOrderId!.Value, cancellationToken);
            if (prod is null) return ApiResponse<QcInspectionDto>.Fail("Production order not found.");
            if (prod.Status != Domain.Entities.ProductionOrderStatus.Completed)
                return ApiResponse<QcInspectionDto>.Fail("QC can only inspect a Completed production order.");

            foreach (var line in cmd.Lines)
            {
                if (!line.ProductId.HasValue)
                    return ApiResponse<QcInspectionDto>.Fail("Finished-goods inspection lines must reference a product.");
                if (line.ProductId.Value != prod.ProductId)
                    return ApiResponse<QcInspectionDto>.Fail(
                        $"Product {line.ProductId} is not the output of production order {prod.Code}.");
            }

            inspectedFromWarehouseId = prod.ReceiveWarehouseId;
        }

        if (inspectedFromWarehouseId == cmd.QuarantineWarehouseId)
            return ApiResponse<QcInspectionDto>.Fail("Quarantine warehouse must differ from the inspected (source) warehouse.");

        var code = await _numbering.NextAsync("QC", null, cancellationToken);

        var entity = new Domain.Entities.QcInspection
        {
            Code = code,
            SourceType = sourceType,
            GoodsReceiptNoteId = sourceType == Domain.Entities.QcSourceType.IncomingMaterial ? cmd.GoodsReceiptNoteId : null,
            ProductionOrderId = sourceType == Domain.Entities.QcSourceType.FinishedGoods ? cmd.ProductionOrderId : null,
            InspectionDate = cmd.InspectionDate,
            InspectedFromWarehouseId = inspectedFromWarehouseId,
            QuarantineWarehouseId = cmd.QuarantineWarehouseId,
            Status = Domain.Entities.QcInspectionStatus.Draft,
            OverallResult = Domain.Entities.QcResult.Passed,
            InspectedBy = cmd.InspectedBy,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.QcInspectionLine
            {
                RawMaterialId = l.RawMaterialId,
                ProductId = l.ProductId,
                InspectedQuantity = l.InspectedQuantity,
                PassedQuantity = l.PassedQuantity,
                RejectedQuantity = l.InspectedQuantity - l.PassedQuantity,
                SortOrder = i,
                DefectNotes = l.DefectNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQcInspectionByIdQuery(entity.Id), cancellationToken);
    }
}
