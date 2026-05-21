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

    public GetSalesOrderByIdQueryHandler(IRepository<Domain.Entities.SalesOrder, long> repo) => _repo = repo;

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

        var lines = so.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new SalesOrderLineDto(
                l.Id, l.ProductId, l.Product.Code, l.Product.Name,
                l.Product.UnitOfMeasure.Code,
                l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                l.SortOrder, l.LineNotes))
            .ToList();

        var totalAmount = lines.Sum(l => l.LineTotal);

        var dto = new SalesOrderDto(
            so.Id, so.Code, so.CustomerId, so.Customer.Code, so.Customer.Name,
            so.OrderDate, so.RequiredDeliveryDate,
            so.CustomerPoRef, so.DeliveryAddress,
            so.Status.ToString(),
            so.CurrencyId, so.Currency.Code, so.Currency.Symbol, so.ExchangeRate,
            so.ConfirmedAt, so.ConfirmedBy, so.Notes,
            totalAmount, totalAmount * so.ExchangeRate, lines);

        return ApiResponse<SalesOrderDto>.Ok(dto);
    }
}
