using BengalTex.ERP.Application.BankReconciliation.Dtos;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.BankReconciliation.Commands;

// ── List statements ──
public sealed record GetBankStatementsQuery(
    PagedQueryParameters Parameters,
    int? BankAccountId = null,
    bool? IsReconciled = null
) : IRequest<ApiResponse<PagedResult<BankStatementListItemDto>>>;

internal sealed class GetBankStatementsQueryHandler
    : IRequestHandler<GetBankStatementsQuery, ApiResponse<PagedResult<BankStatementListItemDto>>>
{
    private readonly IRepository<BankStatement, long> _repo;
    public GetBankStatementsQueryHandler(IRepository<BankStatement, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<BankStatementListItemDto>>> Handle(
        GetBankStatementsQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (request.BankAccountId.HasValue) q = q.Where(s => s.BankAccountId == request.BankAccountId.Value);
        if (request.IsReconciled.HasValue) q = q.Where(s => s.IsReconciled == request.IsReconciled.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(s => s.Code.Contains(search) || s.BankAccount.AccountName.Contains(search));

        q = q.OrderByDescending(s => s.StatementDate);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new BankStatementListItemDto(
                s.Id, s.Code, s.BankAccountId, s.BankAccount.AccountName,
                s.StatementDate, s.PeriodFromDate, s.PeriodToDate,
                s.OpeningBalance, s.ClosingBalance, s.IsReconciled, s.ReconciledAt,
                s.Lines.Count,
                s.Lines.Count(l => l.Status == BankStatementLineStatus.Matched),
                s.Lines.Count(l => l.Status == BankStatementLineStatus.Unmatched)))
            .ToListAsync(ct);

        var result = PagedResult<BankStatementListItemDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, total);
        return ApiResponse<PagedResult<BankStatementListItemDto>>.Ok(result);
    }
}

// ── Get statement detail (header + lines with computed totals) ──
public sealed record GetBankStatementByIdQuery(long Id) : IRequest<ApiResponse<BankStatementDto>>;

internal sealed class GetBankStatementByIdQueryHandler
    : IRequestHandler<GetBankStatementByIdQuery, ApiResponse<BankStatementDto>>
{
    private readonly IRepository<BankStatement, long> _repo;
    public GetBankStatementByIdQueryHandler(IRepository<BankStatement, long> repo) => _repo = repo;

    public async Task<ApiResponse<BankStatementDto>> Handle(GetBankStatementByIdQuery request, CancellationToken ct)
    {
        var row = await _repo.Query()
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
            .Select(s => new
            {
                s.Id, s.Code, s.BankAccountId,
                BankAccountName = s.BankAccount.AccountName,
                s.BankAccount.LedgerAccountId,
                LedgerAccountCode = s.BankAccount.LedgerAccount != null ? s.BankAccount.LedgerAccount.Code : null,
                LedgerAccountName = s.BankAccount.LedgerAccount != null ? s.BankAccount.LedgerAccount.Name : null,
                s.StatementDate, s.PeriodFromDate, s.PeriodToDate,
                s.OpeningBalance, s.ClosingBalance,
                s.IsReconciled, s.ReconciledAt, s.ReconciledBy, s.Notes,
                Lines = s.Lines.OrderBy(l => l.TransactionDate).ThenBy(l => l.Id).Select(l => new
                {
                    l.Id, l.BankStatementId, l.TransactionDate, l.Description, l.ReferenceNumber,
                    l.Amount, l.Status, l.MatchedJournalLineId,
                    MatchedJournalEntryCode = l.MatchedJournalLine != null ? l.MatchedJournalLine.JournalEntry.Code : null,
                    MatchedJournalNarration = l.MatchedJournalLine != null ? l.MatchedJournalLine.JournalEntry.Narration : null,
                    l.MatchedAt, l.MatchedBy, l.Notes
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return ApiResponse<BankStatementDto>.Fail("Bank statement not found.");

        var lines = row.Lines.Select(l => new BankStatementLineDto(
            l.Id, l.BankStatementId, l.TransactionDate, l.Description, l.ReferenceNumber,
            l.Amount, l.Status.ToString(), l.MatchedJournalLineId,
            l.MatchedJournalEntryCode, l.MatchedJournalNarration,
            l.MatchedAt, l.MatchedBy, l.Notes)).ToList();

        var matchedAmount = lines.Where(l => l.Status == "Matched").Sum(l => l.Amount);
        var computedClosing = row.OpeningBalance + matchedAmount;
        var balancesMatch = Math.Round(computedClosing, 2) == Math.Round(row.ClosingBalance, 2);

        return ApiResponse<BankStatementDto>.Ok(new BankStatementDto(
            row.Id, row.Code, row.BankAccountId, row.BankAccountName,
            row.LedgerAccountId, row.LedgerAccountCode, row.LedgerAccountName,
            row.StatementDate, row.PeriodFromDate, row.PeriodToDate,
            row.OpeningBalance, row.ClosingBalance,
            matchedAmount, computedClosing, balancesMatch,
            row.IsReconciled, row.ReconciledAt, row.ReconciledBy, row.Notes,
            lines));
    }
}

// ── Create statement ──
public sealed record CreateBankStatementCommand(
    int BankAccountId,
    DateOnly StatementDate,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateBankStatementCommandValidator : AbstractValidator<CreateBankStatementCommand>
{
    public CreateBankStatementCommandValidator()
    {
        RuleFor(x => x.BankAccountId).GreaterThan(0);
        RuleFor(x => x.StatementDate).NotEmpty();
        RuleFor(x => x.PeriodFromDate).NotEmpty();
        RuleFor(x => x.PeriodToDate).GreaterThanOrEqualTo(x => x.PeriodFromDate);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateBankStatementCommandHandler : IRequestHandler<CreateBankStatementCommand, ApiResponse<long>>
{
    private readonly IRepository<BankStatement, long> _repo;
    private readonly IRepository<BankAccount> _bankRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateBankStatementCommandHandler(IRepository<BankStatement, long> repo,
        IRepository<BankAccount> bankRepo, IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _bankRepo = bankRepo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateBankStatementCommand cmd, CancellationToken ct)
    {
        if (!await _bankRepo.Query().AnyAsync(b => b.Id == cmd.BankAccountId && b.IsActive, ct))
            return ApiResponse<long>.Fail("Bank account not found or inactive.");

        var code = await _numbering.NextAsync("BST", null, ct);
        var s = new BankStatement
        {
            Code = code, BankAccountId = cmd.BankAccountId,
            StatementDate = cmd.StatementDate,
            PeriodFromDate = cmd.PeriodFromDate, PeriodToDate = cmd.PeriodToDate,
            OpeningBalance = cmd.OpeningBalance, ClosingBalance = cmd.ClosingBalance,
            IsReconciled = false,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(s, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(s.Id, "Bank statement created.");
    }
}

// ── Update statement (only if not yet reconciled) ──
public sealed record UpdateBankStatementCommand(
    long Id, DateOnly StatementDate, DateOnly PeriodFromDate, DateOnly PeriodToDate,
    decimal OpeningBalance, decimal ClosingBalance, string? Notes
) : IRequest<ApiResponse>;

internal sealed class UpdateBankStatementCommandHandler : IRequestHandler<UpdateBankStatementCommand, ApiResponse>
{
    private readonly IRepository<BankStatement, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateBankStatementCommandHandler(IRepository<BankStatement, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateBankStatementCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Bank statement not found.");
        if (s.IsReconciled) return ApiResponse.Fail("Cannot edit a reconciled statement.");
        s.StatementDate = cmd.StatementDate;
        s.PeriodFromDate = cmd.PeriodFromDate;
        s.PeriodToDate = cmd.PeriodToDate;
        s.OpeningBalance = cmd.OpeningBalance;
        s.ClosingBalance = cmd.ClosingBalance;
        s.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(s);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Bank statement updated.");
    }
}

// ── Delete statement (only Unreconciled + cascade lines via FK) ──
public sealed record DeleteBankStatementCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteBankStatementCommandHandler : IRequestHandler<DeleteBankStatementCommand, ApiResponse>
{
    private readonly IRepository<BankStatement, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteBankStatementCommandHandler(IRepository<BankStatement, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteBankStatementCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Bank statement not found.");
        if (s.IsReconciled) return ApiResponse.Fail("Cannot delete a reconciled statement.");
        _repo.Remove(s);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Bank statement deleted.");
    }
}

// ── Mark Reconciled ──
public sealed record ReconcileBankStatementCommand(long Id) : IRequest<ApiResponse>;

internal sealed class ReconcileBankStatementCommandHandler : IRequestHandler<ReconcileBankStatementCommand, ApiResponse>
{
    private readonly IRepository<BankStatement, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public ReconcileBankStatementCommandHandler(IRepository<BankStatement, long> repo, IUnitOfWork uow,
        ICurrentUserService currentUser, IDateTimeProvider clock)
    { _repo = repo; _uow = uow; _currentUser = currentUser; _clock = clock; }

    public async Task<ApiResponse> Handle(ReconcileBankStatementCommand cmd, CancellationToken ct)
    {
        var s = await _repo.Query()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Bank statement not found.");
        if (s.IsReconciled) return ApiResponse.Fail("Statement is already reconciled.");

        if (s.Lines.Count == 0)
            return ApiResponse.Fail("Statement has no lines to reconcile.");

        var anyUnmatched = s.Lines.Any(l => l.Status == BankStatementLineStatus.Unmatched);
        if (anyUnmatched)
            return ApiResponse.Fail("All lines must be Matched or Excluded before reconciling.");

        var matchedAmount = s.Lines.Where(l => l.Status == BankStatementLineStatus.Matched).Sum(l => l.Amount);
        var computedClosing = Math.Round(s.OpeningBalance + matchedAmount, 2);
        var stmtClosing = Math.Round(s.ClosingBalance, 2);
        if (computedClosing != stmtClosing)
            return ApiResponse.Fail($"Balances do not match: opening {s.OpeningBalance} + matched {matchedAmount} = {computedClosing}, but statement closing = {stmtClosing}.");

        s.IsReconciled = true;
        s.ReconciledAt = _clock.UtcNow;
        s.ReconciledBy = _currentUser.UserName ?? _currentUser.UserId;
        _repo.Update(s);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Statement {s.Code} reconciled.");
    }
}
