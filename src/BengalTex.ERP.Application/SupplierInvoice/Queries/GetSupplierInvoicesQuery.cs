using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Queries;

public sealed record GetSupplierInvoicesQuery(
    PagedQueryParameters Parameters,
    int? SupplierId = null,
    long? PurchaseOrderId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<SupplierInvoiceListItemDto>>>;

internal sealed class GetSupplierInvoicesQueryHandler
    : IRequestHandler<GetSupplierInvoicesQuery, ApiResponse<PagedResult<SupplierInvoiceListItemDto>>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;

    public GetSupplierInvoicesQueryHandler(IRepository<Domain.Entities.SupplierInvoice, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<SupplierInvoiceListItemDto>>> Handle(
        GetSupplierInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.SupplierId.HasValue)
            query = query.Where(s => s.SupplierId == request.SupplierId.Value);
        if (request.PurchaseOrderId.HasValue)
            query = query.Where(s => s.PurchaseOrderId == request.PurchaseOrderId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.SupplierInvoiceStatus>(request.Status, out var status))
        {
            query = query.Where(s => s.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.Supplier.Code.Contains(search) ||
                s.Supplier.Name.Contains(search) ||
                s.PurchaseOrder.Code.Contains(search) ||
                (s.SupplierInvoiceNumber != null && s.SupplierInvoiceNumber.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(s => s.Code),
            ("code", _)             => query.OrderBy(s => s.Code),
            ("supplier", "desc")    => query.OrderByDescending(s => s.Supplier.Name),
            ("supplier", _)         => query.OrderBy(s => s.Supplier.Name),
            ("invoicedate", "asc")  => query.OrderBy(s => s.InvoiceDate),
            ("invoicedate", _)      => query.OrderByDescending(s => s.InvoiceDate),
            ("duedate", "desc")     => query.OrderByDescending(s => s.DueDate),
            ("duedate", _)          => query.OrderBy(s => s.DueDate),
            ("status", "desc")      => query.OrderByDescending(s => s.Status),
            ("status", _)           => query.OrderBy(s => s.Status),
            _                       => query.OrderByDescending(s => s.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new SupplierInvoiceListItemDto(
                s.Id, s.Code,
                s.SupplierId, s.Supplier.Name,
                s.PurchaseOrderId, s.PurchaseOrder.Code,
                s.SupplierInvoiceNumber,
                s.InvoiceDate, s.DueDate,
                s.Status.ToString(),
                s.Currency.Code, s.ExchangeRate,
                s.VatRate, s.SubtotalAmount, s.VatAmount,
                s.TotalAmount, s.AmountPaid, s.TotalAmount - s.AmountPaid,
                s.TotalAmount * s.ExchangeRate,
                s.Lines.Count))
            .ToListAsync(cancellationToken);

        var result = PagedResult<SupplierInvoiceListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<SupplierInvoiceListItemDto>>.Ok(result);
    }
}
