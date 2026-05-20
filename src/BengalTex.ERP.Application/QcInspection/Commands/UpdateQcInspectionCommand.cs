using BengalTex.ERP.Application.QcInspection.Dtos;
using BengalTex.ERP.Application.QcInspection.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QcInspection.Commands;

/// <summary>
/// Updates a Draft QC inspection. The source (GRN/Production) is fixed at create time —
/// only quarantine warehouse, date, inspector, notes, and lines can change. Lines are
/// fully replaced. Posted inspections are immutable.
/// </summary>
public sealed record UpdateQcInspectionCommand(
    long Id,
    DateOnly InspectionDate,
    int QuarantineWarehouseId,
    string? InspectedBy,
    string? Notes,
    IReadOnlyList<QcInspectionLineInput> Lines
) : IRequest<ApiResponse<QcInspectionDto>>;

public sealed class UpdateQcInspectionCommandValidator : AbstractValidator<UpdateQcInspectionCommand>
{
    public UpdateQcInspectionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.QuarantineWarehouseId).GreaterThan(0);
        RuleFor(x => x.InspectionDate).NotEmpty();
        RuleFor(x => x.InspectedBy).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
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

internal sealed class UpdateQcInspectionCommandHandler
    : IRequestHandler<UpdateQcInspectionCommand, ApiResponse<QcInspectionDto>>
{
    private readonly IRepository<Domain.Entities.QcInspection, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateQcInspectionCommandHandler(
        IRepository<Domain.Entities.QcInspection, long> repo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QcInspectionDto>> Handle(
        UpdateQcInspectionCommand cmd, CancellationToken cancellationToken)
    {
        var insp = await _repo.Query()
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == cmd.Id, cancellationToken);

        if (insp is null) return ApiResponse<QcInspectionDto>.Fail("QC inspection not found.");
        if (insp.Status != Domain.Entities.QcInspectionStatus.Draft)
            return ApiResponse<QcInspectionDto>.Fail("Only draft QC inspections can be edited.");

        var quarantine = await _warehouseRepo.GetByIdAsync(cmd.QuarantineWarehouseId, cancellationToken);
        if (quarantine is null) return ApiResponse<QcInspectionDto>.Fail("Quarantine warehouse not found.");
        if (cmd.QuarantineWarehouseId == insp.InspectedFromWarehouseId)
            return ApiResponse<QcInspectionDto>.Fail("Quarantine warehouse must differ from the inspected (source) warehouse.");

        // Lines must match the inspection's source item type (RM for incoming, Product for FG)
        var expectRm = insp.SourceType == Domain.Entities.QcSourceType.IncomingMaterial;
        foreach (var line in cmd.Lines)
        {
            if (expectRm && !line.RawMaterialId.HasValue)
                return ApiResponse<QcInspectionDto>.Fail("Incoming-material inspection lines must reference a raw material.");
            if (!expectRm && !line.ProductId.HasValue)
                return ApiResponse<QcInspectionDto>.Fail("Finished-goods inspection lines must reference a product.");
        }

        insp.InspectionDate = cmd.InspectionDate;
        insp.QuarantineWarehouseId = cmd.QuarantineWarehouseId;
        insp.InspectedBy = cmd.InspectedBy;
        insp.Notes = cmd.Notes;

        insp.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            insp.Lines.Add(new Domain.Entities.QcInspectionLine
            {
                RawMaterialId = line.RawMaterialId,
                ProductId = line.ProductId,
                InspectedQuantity = line.InspectedQuantity,
                PassedQuantity = line.PassedQuantity,
                RejectedQuantity = line.InspectedQuantity - line.PassedQuantity,
                SortOrder = sortOrder++,
                DefectNotes = line.DefectNotes
            });
        }

        _repo.Update(insp);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQcInspectionByIdQuery(insp.Id), cancellationToken);
    }
}
