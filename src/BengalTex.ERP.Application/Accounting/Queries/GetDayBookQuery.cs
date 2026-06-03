using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>
/// Day Book — every POSTED journal voucher (with its line legs) in the date range, ordered
/// chronologically. The classic "what hit the books today" audit view used by accountants
/// for daily reconciliation. Does NOT compute balances — pure chronological listing.
/// </summary>
public sealed record GetDayBookQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<DayBookDto>>;

internal sealed class GetDayBookQueryHandler
    : IRequestHandler<GetDayBookQuery, ApiResponse<DayBookDto>>
{
    private readonly IRepository<JournalEntry, long> _journalRepo;

    public GetDayBookQueryHandler(IRepository<JournalEntry, long> journalRepo)
        => _journalRepo = journalRepo;

    public async Task<ApiResponse<DayBookDto>> Handle(GetDayBookQuery q, CancellationToken ct)
    {
        var journals = await _journalRepo.Query()
            .Where(j => j.Status == JournalEntryStatus.Posted
                     && j.EntryDate >= q.FromDate
                     && j.EntryDate <= q.ToDate)
            .OrderBy(j => j.EntryDate).ThenBy(j => j.Id)
            .Select(j => new
            {
                j.Id, j.Code, j.EntryDate, j.Reference, j.Narration, j.SourceType, j.SourceCode,
                Lines = j.Lines.OrderBy(l => l.SortOrder).Select(l => new DayBookLineDto(
                    l.AccountId, l.Account.Code, l.Account.Name,
                    l.Debit, l.Credit, l.LineNarration)).ToList()
            })
            .ToListAsync(ct);

        var entries = new List<DayBookEntryDto>(journals.Count);
        decimal grandDebit = 0m, grandCredit = 0m;
        foreach (var j in journals)
        {
            var td = j.Lines.Sum(l => l.Debit);
            var tc = j.Lines.Sum(l => l.Credit);
            grandDebit += td;
            grandCredit += tc;
            entries.Add(new DayBookEntryDto(j.Id, j.Code, j.EntryDate, j.Reference, j.Narration,
                j.SourceType, j.SourceCode, td, tc, j.Lines));
        }

        return ApiResponse<DayBookDto>.Ok(new DayBookDto(q.FromDate, q.ToDate, grandDebit, grandCredit, entries));
    }
}
