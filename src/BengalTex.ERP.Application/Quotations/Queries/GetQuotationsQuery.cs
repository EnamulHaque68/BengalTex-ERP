using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Quotations.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Quotations.Queries;

public sealed record GetQuotationsQuery(
    PagedQueryParameters Parameters,
    int? CustomerId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<QuotationListItemDto>>>;

internal sealed class GetQuotationsQueryHandler
    : IRequestHandler<GetQuotationsQuery, ApiResponse<PagedResult<QuotationListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    public GetQuotationsQueryHandler(IRepository<Domain.Entities.Quotation, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<QuotationListItemDto>>> Handle(
        GetQuotationsQuery request, CancellationToken ct)
    {
        var query = _repo.Query();
        if (request.CustomerId.HasValue) query = query.Where(q => q.CustomerId == request.CustomerId.Value);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<QuotationStatus>(request.Status, out var st))
            query = query.Where(q => q.Status == st);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(q => q.Code.Contains(search) || q.Customer.Name.Contains(search) ||
                (q.CustomerReference != null && q.CustomerReference.Contains(search)));

        query = query.OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(q => new QuotationListItemDto(
                q.Id, q.Code, q.Customer.Name, q.QuotationDate, q.ValidUntil,
                q.Currency.Code, q.TotalAmount, q.Status.ToString(), q.Version, q.Lines.Count))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<QuotationListItemDto>>.Ok(
            PagedResult<QuotationListItemDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, total));
    }
}

public sealed record GetQuotationByIdQuery(long Id) : IRequest<ApiResponse<QuotationDto>>;

internal sealed class GetQuotationByIdQueryHandler
    : IRequestHandler<GetQuotationByIdQuery, ApiResponse<QuotationDto>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    public GetQuotationByIdQueryHandler(IRepository<Domain.Entities.Quotation, long> repo) => _repo = repo;

    public async Task<ApiResponse<QuotationDto>> Handle(GetQuotationByIdQuery request, CancellationToken ct)
    {
        var q = await _repo.Query()
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Currency)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (q is null) return ApiResponse<QuotationDto>.Fail("Quotation not found.");

        var lines = q.Lines.OrderBy(l => l.SortOrder).Select(l => new QuotationLineDto(
            l.Id, l.ProductId, l.Product.Code, l.Product.Name, l.Description, l.Quantity,
            l.MaterialCost, l.LaborCost, l.MachineCost, l.OverheadCost, l.WastagePercent, l.MarginPercent,
            l.UnitCost, l.UnitPrice, l.LineTotal, l.SortOrder)).ToList();

        var dto = new QuotationDto(
            q.Id, q.Code, q.CustomerId, q.Customer.Name, q.QuotationDate, q.ValidUntil,
            q.CurrencyId, q.Currency.Code, q.ExchangeRate, q.Status.ToString(), q.Version, q.RevisionOfId,
            q.TotalAmount, q.CustomerReference, q.Notes, q.SentAt, q.DecidedAt, q.DecidedBy,
            q.ConvertedSalesOrderId, lines);
        return ApiResponse<QuotationDto>.Ok(dto);
    }
}
