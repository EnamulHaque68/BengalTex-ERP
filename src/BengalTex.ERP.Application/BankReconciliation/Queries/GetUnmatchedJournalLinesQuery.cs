using BengalTex.ERP.Application.BankReconciliation.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.BankReconciliation.Queries;

/// <summary>
/// For the rec workspace: posted journal lines on the statement's bank-account ledger that
/// are NOT yet matched to ANY bank statement line. Filtered to the statement's period.
/// </summary>
public sealed record GetUnmatchedJournalLinesQuery(long BankStatementId)
    : IRequest<ApiResponse<IReadOnlyList<UnmatchedJournalLineDto>>>;

internal sealed class GetUnmatchedJournalLinesQueryHandler
    : IRequestHandler<GetUnmatchedJournalLinesQuery, ApiResponse<IReadOnlyList<UnmatchedJournalLineDto>>>
{
    private readonly IRepository<BankStatement, long> _stmtRepo;
    private readonly IRepository<JournalEntryLine, long> _jLineRepo;
    private readonly IRepository<BankStatementLine, long> _bLineRepo;

    public GetUnmatchedJournalLinesQueryHandler(
        IRepository<BankStatement, long> stmtRepo,
        IRepository<JournalEntryLine, long> jLineRepo,
        IRepository<BankStatementLine, long> bLineRepo)
    {
        _stmtRepo = stmtRepo; _jLineRepo = jLineRepo; _bLineRepo = bLineRepo;
    }

    public async Task<ApiResponse<IReadOnlyList<UnmatchedJournalLineDto>>> Handle(
        GetUnmatchedJournalLinesQuery request, CancellationToken ct)
    {
        var stmt = await _stmtRepo.Query()
            .AsNoTracking()
            .Include(s => s.BankAccount)
            .FirstOrDefaultAsync(s => s.Id == request.BankStatementId, ct);
        if (stmt is null)
            return ApiResponse<IReadOnlyList<UnmatchedJournalLineDto>>.Fail("Bank statement not found.");
        if (stmt.BankAccount.LedgerAccountId is null)
            return ApiResponse<IReadOnlyList<UnmatchedJournalLineDto>>.Fail("Bank account is not linked to a ledger account.");

        var ledgerAccountId = stmt.BankAccount.LedgerAccountId.Value;

        // Journal lines on the ledger account that are posted AND in the statement period
        // AND not already matched to ANY statement line (across all statements).
        var matchedJournalLineIds = await _bLineRepo.Query()
            .Where(b => b.MatchedJournalLineId != null)
            .Select(b => b.MatchedJournalLineId!.Value)
            .ToListAsync(ct);
        var matchedSet = matchedJournalLineIds.ToHashSet();

        var rows = await _jLineRepo.Query()
            .AsNoTracking()
            .Where(j => j.AccountId == ledgerAccountId
                     && j.JournalEntry.Status == JournalEntryStatus.Posted
                     && j.JournalEntry.EntryDate >= stmt.PeriodFromDate
                     && j.JournalEntry.EntryDate <= stmt.PeriodToDate)
            .OrderBy(j => j.JournalEntry.EntryDate).ThenBy(j => j.Id)
            .Select(j => new
            {
                j.Id, j.JournalEntryId,
                JournalEntryCode = j.JournalEntry.Code,
                EntryDate = j.JournalEntry.EntryDate,
                Narration = j.JournalEntry.Narration,
                SourceType = j.JournalEntry.SourceType,
                SourceCode = j.JournalEntry.SourceCode,
                j.Debit, j.Credit
            })
            .ToListAsync(ct);

        var items = rows
            .Where(r => !matchedSet.Contains(r.Id))
            .Select(r => new UnmatchedJournalLineDto(
                r.Id, r.JournalEntryId, r.JournalEntryCode, r.EntryDate,
                r.Narration ?? string.Empty, r.SourceType, r.SourceCode,
                r.Debit - r.Credit, r.Debit, r.Credit))
            .ToList();

        return ApiResponse<IReadOnlyList<UnmatchedJournalLineDto>>.Ok(items);
    }
}
