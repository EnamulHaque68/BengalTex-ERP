using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.QcInspection.Dtos;
using BengalTex.ERP.Application.QcInspection.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QcInspection.Commands;

/// <summary>
/// Posts a Draft QC inspection. Two-pass atomic:
///   1. Validate-all — each line's RejectedQuantity ≤ current stock on hand at the source
///      warehouse. Collect violations, fail entire post with consolidated message.
///   2. Apply-all — for each line with RejectedQuantity > 0, move it out of the source
///      warehouse (QcRejectOut) and into the quarantine warehouse (QcRejectIn). Passed
///      quantity is untouched (stays usable in the source warehouse).
///   3. Compute OverallResult (all-passed → Passed, all-rejected → Failed, mixed →
///      PartiallyPassed), flip to Posted.
/// </summary>
public sealed record PostQcInspectionCommand(long Id) : IRequest<ApiResponse<QcInspectionDto>>;

internal sealed class PostQcInspectionCommandHandler
    : IRequestHandler<PostQcInspectionCommand, ApiResponse<QcInspectionDto>>
{
    private readonly IRepository<Domain.Entities.QcInspection, long> _repo;
    private readonly IStockService _stock;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostQcInspectionCommandHandler(
        IRepository<Domain.Entities.QcInspection, long> repo,
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

    public async Task<ApiResponse<QcInspectionDto>> Handle(
        PostQcInspectionCommand cmd, CancellationToken cancellationToken)
    {
        var insp = await _repo.Query()
            .Include(q => q.Lines).ThenInclude(l => l.RawMaterial)
            .Include(q => q.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(q => q.Id == cmd.Id, cancellationToken);

        if (insp is null) return ApiResponse<QcInspectionDto>.Fail("QC inspection not found.");
        if (insp.Status != Domain.Entities.QcInspectionStatus.Draft)
            return ApiResponse<QcInspectionDto>.Fail("Only draft QC inspections can be posted.");
        if (insp.Lines.Count == 0)
            return ApiResponse<QcInspectionDto>.Fail("Cannot post a QC inspection with no lines.");

        // ─── Pass 1: validate rejected qty against source stock ─────────────
        var violations = new List<string>();
        foreach (var line in insp.Lines.Where(l => l.RejectedQuantity > 0m))
        {
            decimal available;
            string itemLabel;
            if (line.RawMaterialId.HasValue)
            {
                available = await _stock.GetRawMaterialOnHandAsync(
                    line.RawMaterialId.Value, insp.InspectedFromWarehouseId, cancellationToken);
                itemLabel = line.RawMaterial?.Name ?? $"RM {line.RawMaterialId}";
            }
            else
            {
                available = await _stock.GetProductOnHandAsync(
                    line.ProductId!.Value, insp.InspectedFromWarehouseId, cancellationToken);
                itemLabel = line.Product?.Name ?? $"Product {line.ProductId}";
            }

            if (line.RejectedQuantity > available)
            {
                violations.Add(
                    $"{itemLabel}: rejected {line.RejectedQuantity:0.####} exceeds available " +
                    $"{available:0.####} at source warehouse.");
            }
        }
        if (violations.Count > 0)
            return ApiResponse<QcInspectionDto>.Fail("Cannot post QC inspection:\n" + string.Join("\n", violations));

        // ─── Pass 2: route rejected qty source → quarantine ─────────────────
        foreach (var line in insp.Lines.Where(l => l.RejectedQuantity > 0m))
        {
            if (line.RawMaterialId.HasValue)
            {
                await _stock.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, insp.InspectedFromWarehouseId, -line.RejectedQuantity,
                    StockMovementType.QcRejectOut, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
                await _stock.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, insp.QuarantineWarehouseId, line.RejectedQuantity,
                    StockMovementType.QcRejectIn, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
            }
            else
            {
                await _stock.PostProductMovementAsync(
                    line.ProductId!.Value, insp.InspectedFromWarehouseId, -line.RejectedQuantity,
                    StockMovementType.QcRejectOut, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
                await _stock.PostProductMovementAsync(
                    line.ProductId.Value, insp.QuarantineWarehouseId, line.RejectedQuantity,
                    StockMovementType.QcRejectIn, "QcInspection", insp.Id, insp.Code,
                    insp.InspectionDate, line.DefectNotes, cancellationToken);
            }
        }

        // Overall result
        var totalRejected = insp.Lines.Sum(l => l.RejectedQuantity);
        var totalPassed = insp.Lines.Sum(l => l.PassedQuantity);
        insp.OverallResult = totalRejected == 0m
            ? Domain.Entities.QcResult.Passed
            : (totalPassed == 0m ? Domain.Entities.QcResult.Failed : Domain.Entities.QcResult.PartiallyPassed);

        insp.Status = Domain.Entities.QcInspectionStatus.Posted;
        insp.InspectedBy ??= _currentUser.UserName;

        _repo.Update(insp);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQcInspectionByIdQuery(insp.Id), cancellationToken);
    }
}
