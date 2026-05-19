using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payment.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payment.Queries;

public sealed record GetPaymentsQuery(
    PagedQueryParameters Parameters,
    long? SupplierInvoiceId = null,
    int? SupplierId = null,
    string? PaymentMethod = null
) : IRequest<ApiResponse<PagedResult<PaymentListItemDto>>>;

internal sealed class GetPaymentsQueryHandler
    : IRequestHandler<GetPaymentsQuery, ApiResponse<PagedResult<PaymentListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Payment, long> _repo;

    public GetPaymentsQueryHandler(IRepository<Domain.Entities.Payment, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<PaymentListItemDto>>> Handle(
        GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.SupplierInvoiceId.HasValue)
            query = query.Where(p => p.SupplierInvoiceId == request.SupplierInvoiceId.Value);
        if (request.SupplierId.HasValue)
            query = query.Where(p => p.SupplierInvoice.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrEmpty(request.PaymentMethod)
            && Enum.TryParse<Domain.Entities.PaymentMethod>(request.PaymentMethod, out var pm))
        {
            query = query.Where(p => p.PaymentMethod == pm);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Code.Contains(search) ||
                p.SupplierInvoice.Code.Contains(search) ||
                p.SupplierInvoice.Supplier.Name.Contains(search) ||
                (p.ReferenceNumber != null && p.ReferenceNumber.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(p => p.Code),
            ("code", _)             => query.OrderBy(p => p.Code),
            ("paymentdate", "asc")  => query.OrderBy(p => p.PaymentDate),
            ("paymentdate", _)      => query.OrderByDescending(p => p.PaymentDate),
            ("amount", "asc")       => query.OrderBy(p => p.Amount),
            ("amount", _)           => query.OrderByDescending(p => p.Amount),
            _                       => query.OrderByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(p => new PaymentListItemDto(
                p.Id, p.Code,
                p.SupplierInvoiceId, p.SupplierInvoice.Code,
                p.SupplierInvoice.SupplierId, p.SupplierInvoice.Supplier.Name,
                p.PaymentDate, p.Amount,
                p.PaymentMethod.ToString(),
                p.ReferenceNumber))
            .ToListAsync(cancellationToken);

        var result = PagedResult<PaymentListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<PaymentListItemDto>>.Ok(result);
    }
}
