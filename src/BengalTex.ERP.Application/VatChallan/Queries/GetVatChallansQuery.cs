using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.VatChallan.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.VatChallan.Queries;

public sealed record GetVatChallansQuery(
    PagedQueryParameters Parameters,
    int? CustomerId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<VatChallanListItemDto>>>;

internal sealed class GetVatChallansQueryHandler
    : IRequestHandler<GetVatChallansQuery, ApiResponse<PagedResult<VatChallanListItemDto>>>
{
    private readonly IRepository<Domain.Entities.VatChallan, long> _repo;

    public GetVatChallansQueryHandler(IRepository<Domain.Entities.VatChallan, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<PagedResult<VatChallanListItemDto>>> Handle(
        GetVatChallansQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.CustomerId.HasValue)
            query = query.Where(v => v.CustomerInvoice.CustomerId == request.CustomerId.Value);
        if (request.FromDate.HasValue)
            query = query.Where(v => v.ChallanDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(v => v.ChallanDate <= request.ToDate.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(v =>
                v.Code.Contains(search) ||
                v.CustomerInvoice.Code.Contains(search) ||
                v.CustomerInvoice.Customer.Name.Contains(search));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")        => query.OrderByDescending(v => v.Code),
            ("code", _)             => query.OrderBy(v => v.Code),
            ("challandate", "asc")  => query.OrderBy(v => v.ChallanDate),
            ("challandate", _)      => query.OrderByDescending(v => v.ChallanDate),
            _                       => query.OrderByDescending(v => v.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(v => new VatChallanListItemDto(
                v.Id, v.Code,
                v.CustomerInvoiceId, v.CustomerInvoice.Code,
                v.CustomerInvoice.CustomerId, v.CustomerInvoice.Customer.Name,
                v.ChallanDate,
                v.SubtotalAmount, v.VatAmount, v.TotalAmount))
            .ToListAsync(cancellationToken);

        var result = PagedResult<VatChallanListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<VatChallanListItemDto>>.Ok(result);
    }
}
