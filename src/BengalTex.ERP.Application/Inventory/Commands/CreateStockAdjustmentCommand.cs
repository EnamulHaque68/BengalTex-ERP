using BengalTex.ERP.Application.Inventory.Dtos;
using BengalTex.ERP.Application.Inventory.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Inventory.Commands;

/// <summary>One raw-material line submitted with a stock-adjustment request.</summary>
public sealed record StockAdjustmentLineInput(
    int RawMaterialId,
    decimal SignedQuantity,
    string? LineNotes);

public sealed record CreateStockAdjustmentCommand(
    DateOnly AdjustmentDate,
    int WarehouseId,
    string Reason,
    string? Notes,
    IReadOnlyList<StockAdjustmentLineInput> Lines
) : IRequest<ApiResponse<StockAdjustmentDto>>;

public sealed class CreateStockAdjustmentCommandValidator : AbstractValidator<CreateStockAdjustmentCommand>
{
    public CreateStockAdjustmentCommandValidator()
    {
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

internal sealed class CreateStockAdjustmentCommandHandler
    : IRequestHandler<CreateStockAdjustmentCommand, ApiResponse<StockAdjustmentDto>>
{
    private readonly IRepository<Domain.Entities.StockAdjustment, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateStockAdjustmentCommandHandler(
        IRepository<Domain.Entities.StockAdjustment, long> repo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StockAdjustmentDto>> Handle(
        CreateStockAdjustmentCommand cmd, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.WarehouseId, cancellationToken);
        if (warehouse is null) return ApiResponse<StockAdjustmentDto>.Fail("Warehouse not found.");

        var rawMaterialIds = cmd.Lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var existingCount = await _rawMaterialRepo.Query()
            .CountAsync(rm => rawMaterialIds.Contains(rm.Id), cancellationToken);
        if (existingCount != rawMaterialIds.Count)
            return ApiResponse<StockAdjustmentDto>.Fail("One or more raw materials not found.");

        var code = await _numbering.NextAsync("ADJ", null, cancellationToken);

        var entity = new Domain.Entities.StockAdjustment
        {
            Code = code,
            AdjustmentDate = cmd.AdjustmentDate,
            WarehouseId = cmd.WarehouseId,
            Reason = cmd.Reason.Trim(),
            Status = Domain.Entities.StockAdjustmentStatus.Draft,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.StockAdjustmentLine
            {
                RawMaterialId = l.RawMaterialId,
                SignedQuantity = l.SignedQuantity,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetStockAdjustmentByIdQuery(entity.Id), cancellationToken);
    }
}
