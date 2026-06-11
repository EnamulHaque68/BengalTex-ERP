using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Queries;

public sealed record GetCustomerInvoiceByIdQuery(long Id) : IRequest<ApiResponse<CustomerInvoiceDto>>;

internal sealed class GetCustomerInvoiceByIdQueryHandler
    : IRequestHandler<GetCustomerInvoiceByIdQuery, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;

    public GetCustomerInvoiceByIdQueryHandler(IRepository<Domain.Entities.CustomerInvoice, long> repo) => _repo = repo;

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        GetCustomerInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.SalesOrder)
            .Include(c => c.Currency)
            .Include(c => c.VatChallan)
            .Include(c => c.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Customer invoice not found.");

        var lines = inv.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new CustomerInvoiceLineDto(
                l.Id, l.ProductId, l.Product.Code, l.Product.Name,
                l.Product.UnitOfMeasure.Code, l.Product.HsCode,
                l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                l.SortOrder, l.LineNotes,
                l.CartonNumberFrom, l.CartonNumberTo, l.UnitsPerCarton,
                l.NetWeightKgPerLine, l.GrossWeightKgPerLine))
            .ToList();

        var dto = new CustomerInvoiceDto(
            inv.Id, inv.Code,
            inv.CustomerId, inv.Customer.Code, inv.Customer.Name,
            inv.SalesOrderId, inv.SalesOrder.Code,
            inv.InvoiceDate, inv.DueDate,
            inv.Status.ToString(),
            inv.CurrencyId, inv.Currency.Code, inv.Currency.Symbol, inv.ExchangeRate,
            inv.VatRate, inv.SubtotalAmount, inv.VatAmount,
            inv.TotalAmount, inv.AmountPaid, inv.TotalAmount - inv.AmountPaid,
            inv.TotalAmount * inv.ExchangeRate,
            inv.IssuedAt, inv.IssuedBy, inv.Notes,
            inv.VatChallan?.Code,
            inv.EpbFormNumber, inv.LcNumber, inv.ShipmentDate,
            inv.IncoTerm, inv.PortOfLoading, inv.PortOfDischarge, inv.VesselName,
            inv.CountryOfDestination, inv.ShippingMarks,
            inv.TotalPackages, inv.GrossWeightKg, inv.NetWeightKg,
            inv.ContainerNumber, inv.SealNumber, inv.TruckNumber,
            lines);

        return ApiResponse<CustomerInvoiceDto>.Ok(dto);
    }
}
