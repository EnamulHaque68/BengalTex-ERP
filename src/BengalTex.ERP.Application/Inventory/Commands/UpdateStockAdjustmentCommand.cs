using BengalTex.ERP.Application.Inventory.Dtos;
using BengalTex.ERP.Application.Inventory.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Inventory.Commands;

public sealed record UpdateStockAdjustmentCommand(
    long Id,
    DateOnly AdjustmentDate,
    int WarehouseId,
    string Reason,
    string? Notes,
    IReadOnlyList<StockAdjustmentLineInput> Lines
) : IRequest<ApiResponse<StockAdjustmentDto>>;

public sealed class UpdateStockAdjustmentCommandValidator : AbstractValidator<UpdateStockAdjustmentCommand>
{
    public UpdateStockAdjustmentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AdjustmentDate).NotEmpty();
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A stock adjustment must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.RawMaterialId).GreaterThan(0);
            line.RuleFor(l => l.SignedQuantity).NotEqual(0m)
                .WithMessage("Signed quantity cannot be zero.");
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("The same raw material appears more than once — combine the quantities.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class UpdateStockAdjustmentCommandHandler
    : IRequestHandler<UpdateStockAdjustmentCommand, ApiResponse<StockAdjustmentDto>>
{
    private readonly IRepository<Domain.Entities.StockAdjustment, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateStockAdjustmentCommandHandler(
        IRepository<Domain.Entities.StockAdjustment, long> repo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StockAdjustmentDto>> Handle(
        UpdateStockAdjustmentCommand cmd, CancellationToken cancellationToken)
    {
        var adj = await _repo.Query()
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.Id, cancellationToken);

        if (adj is null) return ApiResponse<StockAdjustmentDto>.Fail("Stock adjustment not found.");
        if (adj.Status != Domain.Entities.StockAdjustmentStatus.Draft)
            return ApiResponse<StockAdjustmentDto>.Fail("Only draft stock adjustments can be edited.");

        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.WarehouseId, cancellationToken);
        if (warehouse is null) return ApiResponse<StockAdjustmentDto>.Fail("Warehouse not found.");

        var rawMaterialIds = cmd.Lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var existingCount = await _rawMaterialRepo.Query()
            .CountAsync(rm => rawMaterialIds.Contains(rm.Id), cancellationToken);
        if (existingCount != rawMaterialIds.Count)
            return ApiResponse<StockAdjustmentDto>.Fail("One or more raw materials not found.");

        adj.AdjustmentDate = cmd.AdjustmentDate;
        adj.WarehouseId = cmd.WarehouseId;
        adj.Reason = cmd.Reason.Trim();
        adj.Notes = cmd.Notes;

        adj.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            adj.Lines.Add(new Domain.Entities.StockAdjustmentLine
            {
                RawMaterialId = line.RawMaterialId,
                SignedQuantity = line.SignedQuantity,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(adj);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetStockAdjustmentByIdQuery(adj.Id), cancellationToken);
    }
}
