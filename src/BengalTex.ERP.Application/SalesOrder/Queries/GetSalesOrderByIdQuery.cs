using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SalesOrder.Queries;

public sealed record GetSalesOrderByIdQuery(long Id) : IRequest<ApiResponse<SalesOrderDto>>;

internal sealed class GetSalesOrderByIdQueryHandler
    : IRequestHandler<GetSalesOrderByIdQuery, ApiResponse<SalesOrderDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _poRepo;

    public GetSalesOrderByIdQueryHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IRepository<Domain.Entities.ProductionOrder, long> poRepo)
    {
        _repo = repo;
        _poRepo = poRepo;
    }

    public async Task<ApiResponse<SalesOrderDto>> Handle(
        GetSalesOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var so = await _repo.Query()
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Currency)
            .Include(s => s.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (so is null) return ApiResponse<SalesOrderDto>.Fail("Sales order not found.");

        // Phase 1 — pull this SO's linked production orders, then aggregate per line in memory
        // (materialize-then-group avoids the nested-aggregate translation issue).
        var linkedPos = await _poRepo.Query()
            .AsNoTracking()
            .Where(p => p.SalesOrderId == so.Id && p.SalesOrderLineId != null
                && p.Status != Domain.Entities.ProductionOrderStatus.Cancelled)
            .Select(p => new { LineId = p.SalesOrderLineId!.Value, p.Quantity, p.Status })
            .ToListAsync(cancellationToken);

        var byLine = linkedPos
            .GroupBy(x => x.LineId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Allocated: g.Sum(x => x.Quantity),
                    Produced: g.Where(x => x.Status == Domain.Entities.ProductionOrderStatus.Completed)
                               .Sum(x => x.Quantity)));

        var lines = so.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l =>
            {
                var agg = byLine.TryGetValue(l.Id, out var v) ? v : (Allocated: 0m, Produced: 0m);
                return new SalesOrderLineDto(
                    l.Id, l.ProductId, l.Product.Code, l.Product.Name,
                    l.Product.UnitOfMeasure.Code,
                    l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                    l.SortOrder, l.LineNotes,
                    agg.Produced, agg.Allocated);
            })
            .ToList();

        var totalAmount = lines.Sum(l => l.LineTotal);

        var orderedQty = so.Lines.Sum(l => l.Quantity);
        var producedQty = lines.Sum(l => l.ProducedQuantity);
        var productionStatus = ProductionProgressCalc.DeriveStatus(orderedQty, producedQty, linkedPos.Count > 0);
        var productionPercent = ProductionProgressCalc.Percent(orderedQty, producedQty);

        var dto = new SalesOrderDto(
            so.Id, so.Code, so.CustomerId, so.Customer.Code, so.Customer.Name,
            so.OrderDate, so.RequiredDeliveryDate,
            so.CustomerPoRef, so.DeliveryAddress,
            so.Status.ToString(),
            so.CurrencyId, so.Currency.Code, so.Currency.Symbol, so.ExchangeRate,
            so.ConfirmedAt, so.ConfirmedBy, so.Notes,
            totalAmount, totalAmount * so.ExchangeRate, lines,
            orderedQty, producedQty, productionPercent, productionStatus);

        return ApiResponse<SalesOrderDto>.Ok(dto);
    }
}
