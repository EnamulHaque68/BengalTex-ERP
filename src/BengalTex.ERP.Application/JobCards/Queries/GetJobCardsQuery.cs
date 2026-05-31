using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.JobCards.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.JobCards.Queries;

public sealed record GetJobCardsQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    long? ProductionOrderId = null,
    int? MachineId = null,
    int? OperatorEmployeeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<JobCardListItemDto>>>;

internal sealed class GetJobCardsQueryHandler
    : IRequestHandler<GetJobCardsQuery, ApiResponse<PagedResult<JobCardListItemDto>>>
{
    private readonly IRepository<JobCard, long> _repo;
    public GetJobCardsQueryHandler(IRepository<JobCard, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<JobCardListItemDto>>> Handle(
        GetJobCardsQuery request, CancellationToken ct)
    {
        var query = _repo.Query();

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<JobCardStatus>(request.Status, out var s))
            query = query.Where(j => j.Status == s);
        if (request.ProductionOrderId.HasValue) query = query.Where(j => j.ProductionOrderId == request.ProductionOrderId.Value);
        if (request.MachineId.HasValue) query = query.Where(j => j.MachineId == request.MachineId.Value);
        if (request.OperatorEmployeeId.HasValue) query = query.Where(j => j.OperatorEmployeeId == request.OperatorEmployeeId.Value);

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(j => j.CreatedAt >= from);
        }
        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(j => j.CreatedAt <= to);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j =>
                j.Code.Contains(search) ||
                j.ProductionOrder.Code.Contains(search) ||
                (j.BatchNumber != null && j.BatchNumber.Contains(search)));
        }

        query = query.OrderByDescending(j => j.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(j => new JobCardListItemDto(
                j.Id, j.Code, j.ProductionOrderId, j.ProductionOrder.Code,
                j.ProductionOrder.Product.Name,
                j.BatchNumber, j.Quantity, j.CompletedQuantity, j.RejectedQuantity,
                j.Machine != null ? j.Machine.Name : null,
                j.OperatorEmployee != null ? j.OperatorEmployee.FullName : null,
                j.Status.ToString(),
                j.StartedAt, j.CompletedAt, j.ActiveMinutes))
            .ToListAsync(ct);

        var result = PagedResult<JobCardListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<JobCardListItemDto>>.Ok(result);
    }
}

public sealed record GetJobCardBoardCountsQuery : IRequest<ApiResponse<JobCardBoardCountsDto>>;

internal sealed class GetJobCardBoardCountsQueryHandler
    : IRequestHandler<GetJobCardBoardCountsQuery, ApiResponse<JobCardBoardCountsDto>>
{
    private readonly IRepository<JobCard, long> _repo;
    public GetJobCardBoardCountsQueryHandler(IRepository<JobCard, long> repo) => _repo = repo;

    public async Task<ApiResponse<JobCardBoardCountsDto>> Handle(GetJobCardBoardCountsQuery request, CancellationToken ct)
    {
        var byStatus = await _repo.Query()
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        int Get(JobCardStatus s) => byStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0;
        return ApiResponse<JobCardBoardCountsDto>.Ok(new JobCardBoardCountsDto(
            Get(JobCardStatus.Open),
            Get(JobCardStatus.InProgress),
            Get(JobCardStatus.OnHold),
            Get(JobCardStatus.Completed),
            Get(JobCardStatus.Cancelled)));
    }
}
