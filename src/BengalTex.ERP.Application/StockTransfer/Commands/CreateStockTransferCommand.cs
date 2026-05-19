using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.StockTransfer.Dtos;
using BengalTex.ERP.Application.StockTransfer.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.StockTransfer.Commands;

/// <summary>
/// One polymorphic line submitted with a create/update Stock Transfer request.
/// Exactly one of <see cref="RawMaterialId"/> / <see cref="ProductId"/> must be set.
/// </summary>
public sealed record StockTransferLineInput(
    int? RawMaterialId,
    int? ProductId,
    decimal Quantity,
    string? LineNotes);

/// <summary>
/// Creates a Draft Stock Transfer. No stock is moved yet — Draft is editable.
/// Posting via <c>PostStockTransferCommand</c> performs the actual stock movement.
/// </summary>
public sealed record CreateStockTransferCommand(
    int SourceWarehouseId,
    int DestinationWarehouseId,
    DateOnly TransferDate,
    string? Notes,
    IReadOnlyList<StockTransferLineInput> Lines
) : IRequest<ApiResponse<StockTransferDto>>;

public sealed class CreateStockTransferCommandValidator : AbstractValidator<CreateStockTransferCommand>
{
    public CreateStockTransferCommandValidator()
    {
        RuleFor(x => x.SourceWarehouseId).GreaterThan(0);
        RuleFor(x => x.DestinationWarehouseId).GreaterThan(0);
        RuleFor(x => x.DestinationWarehouseId).NotEqual(x => x.SourceWarehouseId)
            .WithMessage("Source and destination warehouses must differ.");
        RuleFor(x => x.TransferDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A stock transfer must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
            line.RuleFor(l => l)
                .Must(l => (l.RawMaterialId.HasValue && !l.ProductId.HasValue)
                        || (!l.RawMaterialId.HasValue && l.ProductId.HasValue))
                .WithMessage("Each line must have exactly one of RawMaterialId or ProductId.");
        });
    }
}

internal sealed class CreateStockTransferCommandHandler
    : IRequestHandler<CreateStockTransferCommand, ApiResponse<StockTransferDto>>
{
    private readonly IRepository<Domain.Entities.StockTransfer, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateStockTransferCommandHandler(
        IRepository<Domain.Entities.StockTransfer, long> repo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _productRepo = productRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StockTransferDto>> Handle(
        CreateStockTransferCommand cmd, CancellationToken cancellationToken)
    {
        var sourceWh = await _warehouseRepo.GetByIdAsync(cmd.SourceWarehouseId, cancellationToken);
        if (sourceWh is null) return ApiResponse<StockTransferDto>.Fail("Source warehouse not found.");
        var destWh = await _warehouseRepo.GetByIdAsync(cmd.DestinationWarehouseId, cancellationToken);
        if (destWh is null) return ApiResponse<StockTransferDto>.Fail("Destination warehouse not found.");

        var rmIds = cmd.Lines.Where(l => l.RawMaterialId.HasValue)
                              .Select(l => l.RawMaterialId!.Value).Distinct().ToList();
        var productIds = cmd.Lines.Where(l => l.ProductId.HasValue)
                                   .Select(l => l.ProductId!.Value).Distinct().ToList();

        if (rmIds.Count > 0)
        {
            var existing = await _rawMaterialRepo.Query()
                .CountAsync(rm => rmIds.Contains(rm.Id), cancellationToken);
            if (existing != rmIds.Count)
                return ApiResponse<StockTransferDto>.Fail("One or more raw materials not found.");
        }
        if (productIds.Count > 0)
        {
            var existing = await _productRepo.Query()
                .CountAsync(p => productIds.Contains(p.Id), cancellationToken);
            if (existing != productIds.Count)
                return ApiResponse<StockTransferDto>.Fail("One or more products not found.");
        }

        var code = await _numbering.NextAsync("TXFR", null, cancellationToken);

        var entity = new Domain.Entities.StockTransfer
        {
            Code = code,
            SourceWarehouseId = cmd.SourceWarehouseId,
            DestinationWarehouseId = cmd.DestinationWarehouseId,
            TransferDate = cmd.TransferDate,
            Status = Domain.Entities.StockTransferStatus.Draft,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.StockTransferLine
            {
                RawMaterialId = l.RawMaterialId,
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetStockTransferByIdQuery(entity.Id), cancellationToken);
    }
}
