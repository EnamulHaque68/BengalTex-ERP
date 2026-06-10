using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Queries;

public sealed record GetCustomerInvoicesQuery(
    PagedQueryParameters Parameters,
    int? CustomerId = null,
    long? SalesOrderId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<CustomerInvoiceListItemDto>>>;

internal sealed class GetCustomerInvoicesQueryHandler
    : IRequestHandler<GetCustomerInvoicesQuery, ApiResponse<PagedResult<CustomerInvoiceListItemDto>>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;

    public GetCustomerInvoicesQueryHandler(IRepository<Domain.Entities.CustomerInvoice, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<CustomerInvoiceListItemDto>>> Handle(
        GetCustomerInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.CustomerId.HasValue)
            query = query.Where(c => c.CustomerId == request.CustomerId.Value);
        if (request.SalesOrderId.HasValue)
            query = query.Where(c => c.SalesOrderId == request.SalesOrderId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.CustomerInvoiceStatus>(request.Status, out var status))
        {
            query = query.Where(c => c.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c =>
                c.Code.Contains(search) ||
                c.Customer.Code.Contains(search) ||
                c.Customer.Name.Contains(search) ||
                c.SalesOrder.Code.Contains(search));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(c => c.Code),
            ("code", _)             => query.OrderBy(c => c.Code),
            ("customer", "desc")    => query.OrderByDescending(c => c.Customer.Name),
            ("customer", _)         => query.OrderBy(c => c.Customer.Name),
            ("invoicedate", "asc")  => query.OrderBy(c => c.InvoiceDate),
            ("invoicedate", _)      => query.OrderByDescending(c => c.InvoiceDate),
            ("duedate", "desc")     => query.OrderByDescending(c => c.DueDate),
            ("duedate", _)          => query.OrderBy(c => c.DueDate),
            ("status", "desc")      => query.OrderByDescending(c => c.Status),
            ("status", _)           => query.OrderBy(c => c.Status),
            _                       => query.OrderByDescending(c => c.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(c => new CustomerInvoiceListItemDto(
                c.Id, c.Code,
                c.CustomerId, c.Customer.Name,
                c.SalesOrderId, c.SalesOrder.Code,
                c.InvoiceDate, c.DueDate,
                c.Status.ToString(),
                c.Currency.Code, c.ExchangeRate,
                c.VatRate, c.SubtotalAmount, c.VatAmount,
                c.TotalAmount, c.AmountPaid, c.TotalAmount - c.AmountPaid,
                c.TotalAmount * c.ExchangeRate,
                c.Lines.Count,
                c.EpbFormNumber, c.ShipmentDate,
                c.Customer.IsExport))
            .ToListAsync(cancellationToken);

        var result = PagedResult<CustomerInvoiceListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<CustomerInvoiceListItemDto>>.Ok(result);
    }
}
