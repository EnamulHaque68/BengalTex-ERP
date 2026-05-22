using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.Subcontract.Dtos;
using BengalTex.ERP.Application.Subcontract.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Subcontract.Commands;

/// <summary>
/// Issues a Draft subcontract order's material out to the subcontractor. Two-pass atomic:
///   1. Validate every line's IssuedQuantity is available at the order warehouse.
///   2. Post a SubcontractIssueOut movement (−) per line; flip to Issued. One SaveChanges.
/// </summary>
public sealed record IssueSubcontractOrderCommand(long Id) : IRequest<ApiResponse<SubcontractOrderDto>>;

internal sealed class IssueSubcontractOrderCommandHandler
    : IRequestHandler<IssueSubcontractOrderCommand, ApiResponse<SubcontractOrderDto>>
{
    private readonly IRepository<SubcontractOrder, long> _repo;
    private readonly IStockService _stock;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public IssueSubcontractOrderCommandHandler(
        IRepository<SubcontractOrder, long> repo, IStockService stock, IUnitOfWork uow,
        ICurrentUserService currentUser, IMediator mediator)
    {
        _repo = repo;
        _stock = stock;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SubcontractOrderDto>> Handle(
        IssueSubcontractOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.Query()
            .Include(s => s.Lines).ThenInclude(l => l.RawMaterial)
            .Include(s => s.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);

        if (order is null) return ApiResponse<SubcontractOrderDto>.Fail("Subcontract order not found.");
        if (order.Status != SubcontractStatus.Draft)
            return ApiResponse<SubcontractOrderDto>.Fail("Only draft subcontract orders can be issued.");
        if (order.Lines.Count == 0)
            return ApiResponse<SubcontractOrderDto>.Fail("Cannot issue an order with no lines.");

        // Pass 1 — validate availability
        var shortages = new List<string>();
        foreach (var line in order.Lines)
        {
            decimal available;
            string label;
            if (line.RawMaterialId.HasValue)
            {
                available = await _stock.GetRawMaterialOnHandAsync(line.RawMaterialId.Value, order.WarehouseId, ct);
                label = line.RawMaterial != null ? $"{line.RawMaterial.Code} ({line.RawMaterial.Name})" : $"RM {line.RawMaterialId}";
            }
            else
            {
                available = await _stock.GetProductOnHandAsync(line.ProductId!.Value, order.WarehouseId, ct);
                label = line.Product != null ? $"{line.Product.Code} ({line.Product.Name})" : $"Product {line.ProductId}";
            }
            if (available < line.IssuedQuantity)
                shortages.Add($"{label}: need {line.IssuedQuantity:0.####}, only {available:0.####} available.");
        }
        if (shortages.Count > 0)
            return ApiResponse<SubcontractOrderDto>.Fail("Insufficient stock to issue:\n" + string.Join("\n", shortages));

        // Pass 2 — move out
        foreach (var line in order.Lines)
        {
            if (line.RawMaterialId.HasValue)
                await _stock.PostRawMaterialMovementAsync(
                    line.RawMaterialId.Value, order.WarehouseId, -line.IssuedQuantity,
                    StockMovementType.SubcontractIssueOut, "SubcontractOrder", order.Id, order.Code,
                    order.OrderDate, line.LineNotes, ct);
            else
                await _stock.PostProductMovementAsync(
                    line.ProductId!.Value, order.WarehouseId, -line.IssuedQuantity,
                    StockMovementType.SubcontractIssueOut, "SubcontractOrder", order.Id, order.Code,
                    order.OrderDate, line.LineNotes, ct);
        }

        order.Status = SubcontractStatus.Issued;
        order.IssuedAt = DateTimeOffset.UtcNow;
        order.IssuedBy = _currentUser.UserName;
        _repo.Update(order);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetSubcontractOrderByIdQuery(order.Id), ct);
    }
}
