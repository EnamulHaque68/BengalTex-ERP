using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Receipt.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Receipt.Queries;

public sealed record GetReceiptsQuery(
    PagedQueryParameters Parameters,
    long? CustomerInvoiceId = null,
    int? CustomerId = null,
    string? PaymentMethod = null
) : IRequest<ApiResponse<PagedResult<ReceiptListItemDto>>>;

internal sealed class GetReceiptsQueryHandler
    : IRequestHandler<GetReceiptsQuery, ApiResponse<PagedResult<ReceiptListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Receipt, long> _repo;

    public GetReceiptsQueryHandler(IRepository<Domain.Entities.Receipt, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<ReceiptListItemDto>>> Handle(
        GetReceiptsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.CustomerInvoiceId.HasValue)
            query = query.Where(r => r.CustomerInvoiceId == request.CustomerInvoiceId.Value);
        if (request.CustomerId.HasValue)
            query = query.Where(r => r.CustomerInvoice.CustomerId == request.CustomerId.Value);

        if (!string.IsNullOrEmpty(request.PaymentMethod)
            && Enum.TryParse<Domain.Entities.PaymentMethod>(request.PaymentMethod, out var pm))
        {
            query = query.Where(r => r.PaymentMethod == pm);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r =>
                r.Code.Contains(search) ||
                r.CustomerInvoice.Code.Contains(search) ||
                r.CustomerInvoice.Customer.Name.Contains(search) ||
                (r.ReferenceNumber != null && r.ReferenceNumber.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(r => r.Code),
            ("code", _)             => query.OrderBy(r => r.Code),
            ("receiptdate", "asc")  => query.OrderBy(r => r.ReceiptDate),
            ("receiptdate", _)      => query.OrderByDescending(r => r.ReceiptDate),
            ("amount", "asc")       => query.OrderBy(r => r.Amount),
            ("amount", _)           => query.OrderByDescending(r => r.Amount),
            _                       => query.OrderByDescending(r => r.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(r => new ReceiptListItemDto(
                r.Id, r.Code,
                r.CustomerInvoiceId, r.CustomerInvoice.Code,
                r.CustomerInvoice.CustomerId, r.CustomerInvoice.Customer.Name,
                r.ReceiptDate, r.Amount,
                r.PaymentMethod.ToString(),
                r.ReferenceNumber))
            .ToListAsync(cancellationToken);

        var result = PagedResult<ReceiptListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<ReceiptListItemDto>>.Ok(result);
    }
}
