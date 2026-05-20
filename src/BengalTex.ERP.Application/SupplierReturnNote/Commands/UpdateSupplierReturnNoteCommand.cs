using BengalTex.ERP.Application.SupplierReturnNote.Dtos;
using BengalTex.ERP.Application.SupplierReturnNote.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierReturnNote.Commands;

public sealed record UpdateSupplierReturnNoteCommand(
    long Id,
    int ReturnFromWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<SupplierReturnNoteLineInput> Lines
) : IRequest<ApiResponse<SupplierReturnNoteDto>>;

public sealed class UpdateSupplierReturnNoteCommandValidator : AbstractValidator<UpdateSupplierReturnNoteCommand>
{
    public UpdateSupplierReturnNoteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateSupplierReturnNoteCommandHandler
    : IRequestHandler<UpdateSupplierReturnNoteCommand, ApiResponse<SupplierReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.SupplierReturnNote, long> _repo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateSupplierReturnNoteCommandHandler(
        IRepository<Domain.Entities.SupplierReturnNote, long> repo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _grnRepo = grnRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierReturnNoteDto>> Handle(
        UpdateSupplierReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var srn = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);

        if (srn is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Supplier return note not found.");
        if (srn.Status != Domain.Entities.SupplierReturnNoteStatus.Draft)
            return ApiResponse<SupplierReturnNoteDto>.Fail("Only draft supplier return notes can be edited.");

        var grn = await _grnRepo.Query()
            .Include(g => g.Lines).ThenInclude(l => l.PurchaseOrderLine).ThenInclude(pol => pol.RawMaterial)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == srn.GoodsReceiptNoteId, cancellationToken);
        if (grn is null) return ApiResponse<SupplierReturnNoteDto>.Fail("Parent goods receipt note not found.");

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

        srn.ReturnFromWarehouseId = cmd.ReturnFromWarehouseId;
        srn.ReturnDate = cmd.ReturnDate;
        srn.VehicleNumber = cmd.VehicleNumber;
        srn.Reason = cmd.Reason;
        srn.Notes = cmd.Notes;

        srn.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            var grnLine = grnLineById[line.GoodsReceiptLineId];
            srn.Lines.Add(new Domain.Entities.SupplierReturnNoteLine
            {
                GoodsReceiptLineId = line.GoodsReceiptLineId,
                RawMaterialId = grnLine.PurchaseOrderLine.RawMaterialId,
                ReturnedQuantity = line.ReturnedQuantity,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(srn);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierReturnNoteByIdQuery(srn.Id), cancellationToken);
    }
}
