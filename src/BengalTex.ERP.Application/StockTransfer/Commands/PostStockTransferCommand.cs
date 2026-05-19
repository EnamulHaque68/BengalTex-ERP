using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.StockTransfer.Dtos;
using BengalTex.ERP.Application.StockTransfer.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.StockTransfer.Commands;

/// <summary>
/// Posts a Draft stock transfer: moves each line's quantity from the source
/// warehouse to the destination. Two-pass atomic:
///   1. Validate-all — check source-warehouse stock availability for EVERY line.
///      Aborts the whole transfer with a consolidated message if any line is short.
///   2. Apply-all — for each line, call <c>IStockService</c> twice (TransferOut at
///      source, TransferIn at destination). Updates header to Posted in the same
///      <c>SaveChanges</c> so the entire transfer commits together or not at all.
///
/// Stock service calls intentionally DON'T call SaveChanges — this handler's
/// single SaveChanges at the end ties the two movements per line + header status
/// flip into one transaction.
/// </summary>
public sealed record PostStockTransferCommand(long Id) : IRequest<ApiResponse<StockTransferDto>>;

internal sealed class PostStockTransferCommandHandler
    : IRequestHandler<PostStockTransferCommand, ApiResponse<StockTransferDto>>
{
    private readonly IRepository<Domain.Entities.StockTransfer, long> _repo;
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostStockTransferCommandHandler(
        IRepository<Domain.Entities.StockTransfer, long> repo,
        IStockService stockService,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _stockService = stockService;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StockTransferDto>> Handle(
        PostStockTransferCommand cmd, CancellationToken cancellationToken)
    {
        var transfer = await _repo.Query()
            .Include(s => s.Lines).ThenInclude(l => l.RawMaterial)
            .Include(s => s.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);

        if (transfer is null) return ApiResponse<StockTransferDto>.Fail("Stock transfer not found.");
        if (transfer.Status != Domain.Entities.StockTransferStatus.Draft)
            return ApiResponse<StockTransferDto>.Fail("Only draft stock transfers can be posted.");
        if (transfer.Lines.Count == 0)
            return ApiResponse<StockTransferDto>.Fail("Cannot post a stock transfer with no lines.");
        if (transfer.SourceWarehouseId == transfer.DestinationWarehouseId)
            return ApiResponse<StockTransferDto>.Fail("Source and destination warehouses must differ.");

        // ─── Pass 1: validate source stock availability for every line ─────
        var shortages = new List<string>();
        foreach (var line in transfer.Lines)
        {
            decimal available;
            string itemLabel;
            string uomCode;

            if (line.RawMaterialId.HasValue)
            {
                available = await _stockService.GetRawMaterialOnHandAsync(
                    line.RawMaterialId.Value, transfer.SourceWarehouseId, cancellationToken);
                itemLabel = line.RawMaterial != null
                    ? $"{line.RawMaterial.Code} ({line.RawMaterial.Name})"
                    : $"RM {line.RawMaterialId}";
                uomCode = line.RawMaterial?.UnitOfMeasureId.ToString() ?? "";
            }
            else
            {
                available = await _stockService.GetProductOnHandAsync(
                    line.ProductId!.Value, transfer.SourceWarehouseId, cancellationToken);
                itemLabel = line.Product != null
                    ? $"{line.Product.Code} ({line.Product.Name})"
                    : $"Product {line.ProductId}";
                uomCode = line.Product?.UnitOfMeasureId.ToString() ?? "";
            }

            if (available < line.Quantity)
            {
                shortages.Add(
                    $"{itemLabel}: need {line.Quantity:0.####}, only {available:0.####} available at source warehouse.");
            }
        }

        if (shortages.Count > 0)
        {
            return ApiResponse<StockTransferDto>.Fail(
                "Insufficient stock at source warehouse:\n" + string.Join("\n", shortages));
        }

        // ─── Pass 2: apply movements (OUT from source, IN at destination) ──
        foreach (var line in transfer.Lines)
        {
            if (line.RawMaterialId.HasValue)
            {
                await _stockService.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, transfer.SourceWarehouseId, -line.Quantity,
                    StockMovementType.TransferOut, "StockTransfer", transfer.Id, transfer.Code,
                    transfer.TransferDate, line.LineNotes, cancellationToken);
                await _stockService.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, transfer.DestinationWarehouseId, line.Quantity,
                    StockMovementType.TransferIn, "StockTransfer", transfer.Id, transfer.Code,
                    transfer.TransferDate, line.LineNotes, cancellationToken);
            }
            else
            {
                await _stockService.PostProductMovementAsync(
                    line.ProductId!.Value, transfer.SourceWarehouseId, -line.Quantity,
                    StockMovementType.TransferOut, "StockTransfer", transfer.Id, transfer.Code,
                    transfer.TransferDate, line.LineNotes, cancellationToken);
                await _stockService.PostProductMovementAsync(
                    line.ProductId.Value, transfer.DestinationWarehouseId, line.Quantity,
                    StockMovementType.TransferIn, "StockTransfer", transfer.Id, transfer.Code,
                    transfer.TransferDate, line.LineNotes, cancellationToken);
            }
        }

        transfer.Status = Domain.Entities.StockTransferStatus.Posted;
        transfer.PostedAt = DateTimeOffset.UtcNow;
        transfer.PostedBy = _currentUser.UserName;

        _repo.Update(transfer);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetStockTransferByIdQuery(transfer.Id), cancellationToken);
    }
}
