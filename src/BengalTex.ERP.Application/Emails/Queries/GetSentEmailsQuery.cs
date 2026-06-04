using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Emails.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Emails.Queries;

public sealed record GetSentEmailsQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    string? SourceType = null,
    long? SourceId = null
) : IRequest<ApiResponse<PagedResult<SentEmailDto>>>;

internal sealed class GetSentEmailsQueryHandler
    : IRequestHandler<GetSentEmailsQuery, ApiResponse<PagedResult<SentEmailDto>>>
{
    private readonly IRepository<SentEmail, long> _repo;
    public GetSentEmailsQueryHandler(IRepository<SentEmail, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<SentEmailDto>>> Handle(
        GetSentEmailsQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(req.Status)
            && Enum.TryParse<SentEmailStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (!string.IsNullOrEmpty(req.SourceType)) q = q.Where(x => x.SourceType == req.SourceType);
        if (req.SourceId.HasValue) q = q.Where(x => x.SourceId == req.SourceId.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Subject.Contains(search)
                          || x.ToAddresses.Contains(search)
                          || (x.SourceCode != null && x.SourceCode.Contains(search)));

        q = q.OrderByDescending(x => x.SentAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(x => new SentEmailDto(
                x.Id, x.SentAt, x.SentByUser,
                x.SourceType, x.SourceId, x.SourceCode,
                x.ToAddresses, x.CcAddresses, x.Subject,
                x.Status.ToString(), x.ErrorMessage))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<SentEmailDto>>.Ok(
            PagedResult<SentEmailDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}
