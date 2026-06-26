using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Mrp.Queries;
using BengalTex.ERP.Application.PurchaseRequisitions.Commands;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Mrp.Commands;

/// <summary>
/// Raises ONE draft Purchase Requisition covering the MRP shortages for the selected raw materials
/// (Phase 3). Recomputes MRP server-side (so the quantities can't be tampered with), keeps only the
/// requested raw materials that still have a shortage, and reuses <see cref="CreatePurchaseRequisitionCommand"/>.
/// Estimated unit price = the material's weighted-average cost. Returns the new PR id.
/// </summary>
public sealed record GeneratePurchaseRequisitionFromMrpCommand(
    IReadOnlyList<int> RawMaterialIds) : IRequest<ApiResponse<long>>;

internal sealed class GeneratePurchaseRequisitionFromMrpCommandHandler
    : IRequestHandler<GeneratePurchaseRequisitionFromMrpCommand, ApiResponse<long>>
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public GeneratePurchaseRequisitionFromMrpCommandHandler(
        IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<long>> Handle(
        GeneratePurchaseRequisitionFromMrpCommand cmd, CancellationToken ct)
    {
        if (cmd.RawMaterialIds is null || cmd.RawMaterialIds.Count == 0)
            return ApiResponse<long>.Fail("Select at least one raw material with a shortage.");

        var mrp = await _mediator.Send(new GetMrpQuery(ShortageOnly: true), ct);
        if (!mrp.Success || mrp.Data is null)
            return ApiResponse<long>.Fail(mrp.Message ?? "Could not compute MRP.");

        var requested = cmd.RawMaterialIds.ToHashSet();
        var lines = mrp.Data.Items
            .Where(i => requested.Contains(i.RawMaterialId) && i.ShortageQuantity > 0m)
            .Select(i => new PurchaseRequisitionLineInput(
                i.RawMaterialId,
                i.ShortageQuantity,
                i.EstimatedUnitPrice,
                $"MRP shortage — required {i.RequiredQuantity:0.####}, on hand {i.OnHandQuantity:0.####}, incoming {i.IncomingQuantity:0.####}"))
            .ToList();

        if (lines.Count == 0)
            return ApiResponse<long>.Fail("None of the selected materials currently have a shortage.");

        return await _mediator.Send(new CreatePurchaseRequisitionCommand(
            RequisitionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NeededByDate: null,
            DepartmentId: null,
            DepartmentText: "MRP",
            RequestedBy: _currentUser.UserName,
            Purpose: "Auto-generated from MRP material shortage",
            Notes: null,
            Lines: lines), ct);
    }
}
