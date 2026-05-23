using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Samples.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Samples.Queries;

public sealed record GetSamplesQuery(
    PagedQueryParameters Parameters,
    int? CustomerId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<SampleListItemDto>>>;

internal sealed class GetSamplesQueryHandler
    : IRequestHandler<GetSamplesQuery, ApiResponse<PagedResult<SampleListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    public GetSamplesQueryHandler(IRepository<Domain.Entities.Sample, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<SampleListItemDto>>> Handle(GetSamplesQuery request, CancellationToken ct)
    {
        var query = _repo.Query();
        if (request.CustomerId.HasValue) query = query.Where(s => s.CustomerId == request.CustomerId.Value);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<SampleStatus>(request.Status, out var st))
            query = query.Where(s => s.Status == st);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Code.Contains(search) || s.Title.Contains(search) ||
                s.Customer.Name.Contains(search) || (s.BuyerReference != null && s.BuyerReference.Contains(search)));

        query = query.OrderByDescending(s => s.RequestedDate).ThenByDescending(s => s.Id);

        var total = await query.CountAsync(ct);
        var rows = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new
            {
                s.Id, s.Code, CustomerName = s.Customer.Name,
                s.Title, ProductName = s.Product != null ? s.Product.Name : null,
                s.Quantity, s.RequestedDate, s.TargetDate, s.SubmittedDate, s.Status
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new SampleListItemDto(
            r.Id, r.Code, r.CustomerName, r.Title, r.ProductName, r.Quantity,
            r.RequestedDate, r.TargetDate, r.Status.ToString(),
            r.SubmittedDate.HasValue ? r.SubmittedDate.Value.DayNumber - r.RequestedDate.DayNumber : null)).ToList();

        return ApiResponse<PagedResult<SampleListItemDto>>.Ok(
            PagedResult<SampleListItemDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, total));
    }
}

public sealed record GetSampleByIdQuery(long Id) : IRequest<ApiResponse<SampleDto>>;

internal sealed class GetSampleByIdQueryHandler : IRequestHandler<GetSampleByIdQuery, ApiResponse<SampleDto>>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    public GetSampleByIdQueryHandler(IRepository<Domain.Entities.Sample, long> repo) => _repo = repo;

    public async Task<ApiResponse<SampleDto>> Handle(GetSampleByIdQuery request, CancellationToken ct)
    {
        var r = await _repo.Query()
            .Where(s => s.Id == request.Id)
            .Select(s => new
            {
                s.Id, s.Code, s.CustomerId, CustomerName = s.Customer.Name,
                s.ProductId, ProductName = s.Product != null ? s.Product.Name : null,
                s.StyleId, StyleName = s.Style != null ? s.Style.StyleName : null,
                s.Title, s.Description, s.BuyerReference, s.Quantity, s.RequestedDate, s.TargetDate,
                s.Status, s.SubmittedDate, s.DecidedAt, s.DecidedBy, s.Feedback, s.Notes
            })
            .FirstOrDefaultAsync(ct);
        if (r is null) return ApiResponse<SampleDto>.Fail("Sample not found.");

        var lead = r.SubmittedDate.HasValue ? r.SubmittedDate.Value.DayNumber - r.RequestedDate.DayNumber : (int?)null;
        return ApiResponse<SampleDto>.Ok(new SampleDto(
            r.Id, r.Code, r.CustomerId, r.CustomerName, r.ProductId, r.ProductName, r.StyleId, r.StyleName,
            r.Title, r.Description, r.BuyerReference, r.Quantity, r.RequestedDate, r.TargetDate,
            r.Status.ToString(), r.SubmittedDate, r.DecidedAt, r.DecidedBy, r.Feedback, lead, r.Notes));
    }
}
