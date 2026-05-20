using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierReturnNote.Dtos;
using BengalTex.ERP.Application.SupplierReturnNote.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierReturnNote.Commands;

public sealed record SupplierReturnNoteLineInput(
    long GoodsReceiptLineId,
    decimal ReturnedQuantity,
    string? LineNotes);

public sealed record CreateSupplierReturnNoteCommand(
    long GoodsReceiptNoteId,
    int ReturnFromWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<SupplierReturnNoteLineInput> Lines
) : IRequest<ApiResponse<SupplierReturnNoteDto>>;

public sealed class CreateSupplierReturnNoteCommandValidator : AbstractValidator<CreateSupplierReturnNoteCommand>
{
    public CreateSupplierReturnNoteCommandValidator()
    {
        RuleFor(x => x.GoodsReceiptNoteId).GreaterThan(0);
        RuleFor(x => x.ReturnFromWarehouseId).GreaterThan(0);
        RuleFor(x => x.ReturnDate).NotEmpty();
        RuleFor(x => x.VehicleNumber).MaximumLength(50);
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A supplier return note must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.GoodsReceiptLineId).GreaterThan(0);
            line.RuleFor(l => l.ReturnedQuantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.GoodsReceiptLineId).Distinct().Count() == lines.Count)
            .WithMessage("The same GRN line appears more than once.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateSupplierReturnNoteCommandHandler
    : IRequestHandler<CreateSupplierReturnNoteCommand, ApiResponse<SupplierReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.SupplierReturnNote, long> _repo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateSupplierReturnNoteCommandHandler(
        IRepository<Domain.Entities.SupplierReturnNote, long> repo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _grnRepo = grnRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierReturnNoteDto>> Handle(
        CreateSupplierReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var grn = await _grnRepo.Query()
            .Include(g => g.Lines).ThenInclude(l => l.PurchaseOrderLine).ThenInclude(pol => pol.RawMaterial)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == cmd.GoodsReceiptNoteId, cancellationToken);

        if (grn is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Goods receipt note not found.");
        if (grn.Status != Domain.Entities.GoodsReceiptStatus.Posted)
            return ApiResponse<SupplierReturnNoteDto>.Fail("Supplier returns can only be recorded against a Posted goods receipt note.");

        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.ReturnFromWarehouseId, cancellationToken);
        if (warehouse is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Return-from warehouse not found.");

        var grnLineById = grn.Lines.ToDictionary(l => l.Id);
        foreach (var input in cmd.Lines)
        {
            if (!grnLineById.TryGetValue(input.GoodsReceiptLineId, out var grnLine))
                return ApiResponse<SupplierReturnNoteDto>.Fail(
                    $"GRN line {input.GoodsReceiptLineId} does not belong to GRN {grn.Code}.");

            var available = grnLine.ReceivedQuantity - grnLine.ReturnedQuantity;
            if (input.ReturnedQuantity > available)
            {
                return ApiResponse<SupplierReturnNoteDto>.Fail(
                    $"{grnLine.PurchaseOrderLine.RawMaterial.Name}: return qty {input.ReturnedQuantity:0.####} " +
                    $"exceeds available {available:0.####} (received {grnLine.ReceivedQuantity:0.####}, " +
                    $"already returned {grnLine.ReturnedQuantity:0.####}).");
            }
        }

        var code = await _numbering.NextAsync("SRN", null, cancellationToken);

        var entity = new Domain.Entities.SupplierReturnNote
        {
            Code = code,
            GoodsReceiptNoteId = cmd.GoodsReceiptNoteId,
            ReturnDate = cmd.ReturnDate,
            ReturnFromWarehouseId = cmd.ReturnFromWarehouseId,
            Status = Domain.Entities.SupplierReturnNoteStatus.Draft,
            VehicleNumber = cmd.VehicleNumber,
            Reason = cmd.Reason,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) =>
            {
                var grnLine = grnLineById[l.GoodsReceiptLineId];
                return new Domain.Entities.SupplierReturnNoteLine
                {
                    GoodsReceiptLineId = l.GoodsReceiptLineId,
                    RawMaterialId = grnLine.PurchaseOrderLine.RawMaterialId,
                    ReturnedQuantity = l.ReturnedQuantity,
                    SortOrder = i,
                    LineNotes = l.LineNotes
                };
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierReturnNoteByIdQuery(entity.Id), cancellationToken);
    }
}
