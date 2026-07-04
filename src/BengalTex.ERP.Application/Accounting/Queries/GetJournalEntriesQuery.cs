using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

public sealed record GetJournalEntriesQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? VoucherType = null          // Phase A1 — filter by voucher classification
) : IRequest<ApiResponse<PagedResult<JournalEntryListItemDto>>>;

internal sealed class GetJournalEntriesQueryHandler
    : IRequestHandler<GetJournalEntriesQuery, ApiResponse<PagedResult<JournalEntryListItemDto>>>
{
    private readonly IRepository<JournalEntry, long> _repo;

    public GetJournalEntriesQueryHandler(IRepository<JournalEntry, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<JournalEntryListItemDto>>> Handle(
        GetJournalEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<JournalEntryStatus>(request.Status, out var status))
            query = query.Where(j => j.Status == status);
        if (!string.IsNullOrEmpty(request.VoucherType)
            && Enum.TryParse<VoucherType>(request.VoucherType, out var vType))
            query = query.Where(j => j.VoucherType == vType);
        if (request.FromDate.HasValue) query = query.Where(j => j.EntryDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(j => j.EntryDate <= request.ToDate.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(j =>
                j.Code.Contains(search) ||
                (j.Reference != null && j.Reference.Contains(search)) ||
                (j.Narration != null && j.Narration.Contains(search)));

        query = query.OrderByDescending(j => j.EntryDate).ThenByDescending(j => j.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(j => new JournalEntryListItemDto(
                j.Id, j.Code, j.EntryDate, j.Reference, j.Narration, j.Status.ToString(),
                j.Lines.Sum(l => l.Debit), j.Lines.Count, j.SourceType, j.SourceCode,
                j.VoucherType.ToString()))
            .ToListAsync(cancellationToken);

        var result = PagedResult<JournalEntryListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<JournalEntryListItemDto>>.Ok(result);
    }
}

public sealed record GetJournalEntryByIdQuery(long Id) : IRequest<ApiResponse<JournalEntryDto>>;

internal sealed class GetJournalEntryByIdQueryHandler
    : IRequestHandler<GetJournalEntryByIdQuery, ApiResponse<JournalEntryDto>>
{
    private readonly IRepository<JournalEntry, long> _repo;

    public GetJournalEntryByIdQueryHandler(IRepository<JournalEntry, long> repo) => _repo = repo;

    public async Task<ApiResponse<JournalEntryDto>> Handle(
        GetJournalEntryByIdQuery request, CancellationToken cancellationToken)
    {
        var j = await _repo.Query()
            .AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.Account)
            .Include(x => x.ReversedEntry)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (j is null) return ApiResponse<JournalEntryDto>.Fail("Journal voucher not found.");

        var lines = j.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new JournalEntryLineDto(
                l.Id, l.AccountId, l.Account.Code, l.Account.Name,
                l.Debit, l.Credit, l.LineNarration, l.SortOrder))
            .ToList();

        var dto = new JournalEntryDto(
            j.Id, j.Code, j.EntryDate, j.Reference, j.Narration, j.Status.ToString(),
            j.SourceType, j.SourceId, j.SourceCode, j.PostedAt, j.PostedBy,
            lines.Sum(l => l.Debit), lines.Sum(l => l.Credit), lines,
            j.VoucherType.ToString(), j.ReversedEntryId,
            j.ReversedEntry != null ? j.ReversedEntry.Code : null, j.ReversalReason);

        return ApiResponse<JournalEntryDto>.Ok(dto);
    }
}
