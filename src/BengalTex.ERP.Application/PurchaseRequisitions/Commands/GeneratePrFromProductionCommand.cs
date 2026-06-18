using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.PurchaseRequisitions.Commands;

/// <summary>
/// Material-shortage → Purchase Requisition automation. For a production order it explodes the BOM
/// (qty × (1 + wastage%) × scale), compares each raw material's requirement against total on-hand,
/// and raises a DRAFT Purchase Requisition for the shortfalls (estimated at the RM's weighted-average
/// cost, falling back to standard cost). Returns the new PR id, or a friendly fail when nothing is short.
/// </summary>
public sealed record GeneratePrFromProductionCommand(long ProductionOrderId) : IRequest<ApiResponse<long>>;

internal sealed class GeneratePrFromProductionCommandHandler
    : IRequestHandler<GeneratePrFromProductionCommand, ApiResponse<long>>
{
    private readonly IRepository<ProductionOrder, long> _poRepo;
    private readonly IStockService _stock;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public GeneratePrFromProductionCommandHandler(
        IRepository<ProductionOrder, long> poRepo, IStockService stock,
        ICurrentUserService currentUser, IMediator mediator)
    {
        _poRepo = poRepo; _stock = stock; _currentUser = currentUser; _mediator = mediator;
    }

    public async Task<ApiResponse<long>> Handle(GeneratePrFromProductionCommand cmd, CancellationToken ct)
    {
        var po = await _poRepo.Query().AsNoTracking()
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.RawMaterial)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProductionOrderId, ct);

        if (po is null) return ApiResponse<long>.Fail("Production order not found.");
        if (po.Bom is null || po.Bom.Lines.Count == 0)
            return ApiResponse<long>.Fail("This production order's BOM has no lines.");
        if (po.Bom.OutputQuantity <= 0m)
            return ApiResponse<long>.Fail("BOM output quantity must be greater than zero.");

        var scale = po.Quantity / po.Bom.OutputQuantity;

        var lines = new List<PurchaseRequisitionLineInput>();
        foreach (var bl in po.Bom.Lines)
        {
            var required = bl.Quantity * (1 + bl.WastagePercent / 100m) * scale;
            var available = await _stock.GetRawMaterialTotalOnHandAsync(bl.RawMaterialId, ct);
            var shortfall = required - available;
            if (shortfall <= 0m) continue;

            var price = bl.RawMaterial.WeightedAverageCost > 0m
                ? bl.RawMaterial.WeightedAverageCost
                : bl.RawMaterial.StandardCost;

            lines.Add(new PurchaseRequisitionLineInput(
                bl.RawMaterialId, Math.Round(shortfall, 4, MidpointRounding.AwayFromZero), price,
                $"Shortfall for production {po.Code} (need {required:0.####}, have {available:0.####})"));
        }

        if (lines.Count == 0)
            return ApiResponse<long>.Fail("No material shortage — sufficient stock on hand for this production order.");

        return await _mediator.Send(new CreatePurchaseRequisitionCommand(
            DateOnly.FromDateTime(DateTime.UtcNow), null, null, "Production",
            _currentUser.UserName, $"Material shortfall for production {po.Code}", null, lines), ct);
    }
}
