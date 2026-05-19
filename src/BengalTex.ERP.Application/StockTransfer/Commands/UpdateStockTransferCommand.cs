using BengalTex.ERP.Application.StockTransfer.Dtos;
using BengalTex.ERP.Application.StockTransfer.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.StockTransfer.Commands;

/// <summary>
/// Updates a Draft Stock Transfer. Lines are fully replaced (clear-and-recreate)
/// like other transactional documents. Posted transfers are immutable.
/// </summary>
public sealed record UpdateStockTransferCommand(
    long Id,
    int SourceWarehouseId,
    int DestinationWarehouseId,
    DateOnly TransferDate,
    string? Notes,
    IReadOnlyList<StockTransferLineInput> Lines
) : IRequest<ApiResponse<StockTransferDto>>;

public sealed class UpdateStockTransferCommandValidator : AbstractValidator<UpdateStockTransferCommand>
{
    public UpdateStockTransferCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateStockTransferCommandHandler
    : IRequestHandler<UpdateStockTransferCommand, ApiResponse<StockTransferDto>>
{
    private readonly IRepository<Domain.Entities.StockTransfer, long> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateStockTransferCommandHandler(
        IRepository<Domain.Entities.StockTransfer, long> repo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _rawMaterialRepo = rawMaterialRepo;
        _productRepo = productRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StockTransferDto>> Handle(
        UpdateStockTransferCommand cmd, CancellationToken cancellationToken)
    {
        var transfer = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);

        if (transfer is null) return ApiResponse<StockTransferDto>.Fail("Stock transfer not found.");
        if (transfer.Status != Domain.Entities.StockTransferStatus.Draft)
            return ApiResponse<StockTransferDto>.Fail("Only draft stock transfers can be edited.");

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

        transfer.SourceWarehouseId = cmd.SourceWarehouseId;
        transfer.DestinationWarehouseId = cmd.DestinationWarehouseId;
        transfer.TransferDate = cmd.TransferDate;
        transfer.Notes = cmd.Notes;

        transfer.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            transfer.Lines.Add(new Domain.Entities.StockTransferLine
            {
                RawMaterialId = line.RawMaterialId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(transfer);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetStockTransferByIdQuery(transfer.Id), cancellationToken);
    }
}
