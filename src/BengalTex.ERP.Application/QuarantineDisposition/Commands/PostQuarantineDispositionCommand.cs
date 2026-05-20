using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.QuarantineDisposition.Dtos;
using BengalTex.ERP.Application.QuarantineDisposition.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QuarantineDisposition.Commands;

/// <summary>
/// Posts a Draft quarantine disposition. Two-pass atomic:
///   1. Validate-all — each line's quantity ≤ current stock on hand in the quarantine
///      warehouse. Collect violations, fail entire post with consolidated message.
///   2. Apply-all — per line:
///        - Release: QuarantineReleaseOut from quarantine + QuarantineReleaseIn at destination.
///        - Scrap:   Scrap movement out of quarantine (write-off, no destination).
///   3. Flip to Posted, set PostedAt/By.
/// </summary>
public sealed record PostQuarantineDispositionCommand(long Id) : IRequest<ApiResponse<QuarantineDispositionDto>>;

internal sealed class PostQuarantineDispositionCommandHandler
    : IRequestHandler<PostQuarantineDispositionCommand, ApiResponse<QuarantineDispositionDto>>
{
    private readonly IRepository<Domain.Entities.QuarantineDisposition, long> _repo;
    private readonly IStockService _stock;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostQuarantineDispositionCommandHandler(
        IRepository<Domain.Entities.QuarantineDisposition, long> repo,
        IStockService stock,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _stock = stock;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QuarantineDispositionDto>> Handle(
        PostQuarantineDispositionCommand cmd, CancellationToken cancellationToken)
    {
        var disp = await _repo.Query()
            .Include(d => d.Lines).ThenInclude(l => l.RawMaterial)
            .Include(d => d.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(d => d.Id == cmd.Id, cancellationToken);

        if (disp is null) return ApiResponse<QuarantineDispositionDto>.Fail("Quarantine disposition not found.");
        if (disp.Status != Domain.Entities.QuarantineDispositionStatus.Draft)
            return ApiResponse<QuarantineDispositionDto>.Fail("Only draft dispositions can be posted.");
        if (disp.Lines.Count == 0)
            return ApiResponse<QuarantineDispositionDto>.Fail("Cannot post a disposition with no lines.");

        var isRelease = disp.DispositionType == Domain.Entities.DispositionType.Release;
        if (isRelease && !disp.DestinationWarehouseId.HasValue)
            return ApiResponse<QuarantineDispositionDto>.Fail("Release disposition has no destination warehouse.");

        // ─── Pass 1: validate quarantine stock availability ─────────────────
        var violations = new List<string>();
        foreach (var line in disp.Lines)
        {
            decimal available;
            string itemLabel;
            if (line.RawMaterialId.HasValue)
            {
                available = await _stock.GetRawMaterialOnHandAsync(
                    line.RawMaterialId.Value, disp.QuarantineWarehouseId, cancellationToken);
                itemLabel = line.RawMaterial?.Name ?? $"RM {line.RawMaterialId}";
            }
            else
            {
                available = await _stock.GetProductOnHandAsync(
                    line.ProductId!.Value, disp.QuarantineWarehouseId, cancellationToken);
                itemLabel = line.Product?.Name ?? $"Product {line.ProductId}";
            }

            if (line.Quantity > available)
                violations.Add($"{itemLabel}: dispose {line.Quantity:0.####} exceeds available {available:0.####} in quarantine.");
        }
        if (violations.Count > 0)
            return ApiResponse<QuarantineDispositionDto>.Fail("Cannot post disposition:\n" + string.Join("\n", violations));

        // ─── Pass 2: apply movements ────────────────────────────────────────
        foreach (var line in disp.Lines)
        {
            if (line.RawMaterialId.HasValue)
            {
                await _stock.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, disp.QuarantineWarehouseId, -line.Quantity,
                    isRelease ? StockMovementType.QuarantineReleaseOut : StockMovementType.Scrap,
                    "QuarantineDisposition", disp.Id, disp.Code, disp.DispositionDate, line.LineNotes, cancellationToken);
                if (isRelease)
                    await _stock.PostRawMaterialMovementAsync(
                        line.RawMaterialId.Value, disp.DestinationWarehouseId!.Value, line.Quantity,
                        StockMovementType.QuarantineReleaseIn,
                        "QuarantineDisposition", disp.Id, disp.Code, disp.DispositionDate, line.LineNotes, cancellationToken);
            }
            else
            {
                await _stock.PostProductMovementAsync(
                    line.ProductId!.Value, disp.QuarantineWarehouseId, -line.Quantity,
                    isRelease ? StockMovementType.QuarantineReleaseOut : StockMovementType.Scrap,
                    "QuarantineDisposition", disp.Id, disp.Code, disp.DispositionDate, line.LineNotes, cancellationToken);
                if (isRelease)
                    await _stock.PostProductMovementAsync(
                        line.ProductId.Value, disp.DestinationWarehouseId!.Value, line.Quantity,
                        StockMovementType.QuarantineReleaseIn,
                        "QuarantineDisposition", disp.Id, disp.Code, disp.DispositionDate, line.LineNotes, cancellationToken);
            }
        }

        disp.Status = Domain.Entities.QuarantineDispositionStatus.Posted;
        disp.PostedAt = DateTimeOffset.UtcNow;
        disp.PostedBy = _currentUser.UserName;

        _repo.Update(disp);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQuarantineDispositionByIdQuery(disp.Id), cancellationToken);
    }
}
