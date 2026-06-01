using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.BankReconciliation.Commands;

// ── Add line to statement ──
public sealed record AddStatementLineCommand(
    long BankStatementId,
    DateOnly TransactionDate,
    string Description,
    string? ReferenceNumber,
    decimal Amount,                  // signed
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class AddStatementLineCommandValidator : AbstractValidator<AddStatementLineCommand>
{
    public AddStatementLineCommandValidator()
    {
        RuleFor(x => x.BankStatementId).GreaterThan(0);
        RuleFor(x => x.TransactionDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class AddStatementLineCommandHandler : IRequestHandler<AddStatementLineCommand, ApiResponse<long>>
{
    private readonly IRepository<BankStatementLine, long> _repo;
    private readonly IRepository<BankStatement, long> _stmtRepo;
    private readonly IUnitOfWork _uow;

    public AddStatementLineCommandHandler(IRepository<BankStatementLine, long> repo,
        IRepository<BankStatement, long> stmtRepo, IUnitOfWork uow)
    { _repo = repo; _stmtRepo = stmtRepo; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(AddStatementLineCommand cmd, CancellationToken ct)
    {
        var stmt = await _stmtRepo.GetByIdAsync(cmd.BankStatementId, ct);
        if (stmt is null) return ApiResponse<long>.Fail("Bank statement not found.");
        if (stmt.IsReconciled) return ApiResponse<long>.Fail("Cannot add lines to a reconciled statement.");

        var line = new BankStatementLine
        {
            BankStatementId = cmd.BankStatementId,
            TransactionDate = cmd.TransactionDate,
            Description = cmd.Description.Trim(),
            ReferenceNumber = string.IsNullOrWhiteSpace(cmd.ReferenceNumber) ? null : cmd.ReferenceNumber.Trim(),
            Amount = cmd.Amount,
            Status = BankStatementLineStatus.Unmatched,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(line, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(line.Id, "Statement line added.");
    }
}

// ── Update line (Unmatched only) ──
public sealed record UpdateStatementLineCommand(
    long Id, DateOnly TransactionDate, string Description, string? ReferenceNumber, decimal Amount, string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateStatementLineCommandValidator : AbstractValidator<UpdateStatementLineCommand>
{
    public UpdateStatementLineCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateStatementLineCommandHandler : IRequestHandler<UpdateStatementLineCommand, ApiResponse>
{
    private readonly IRepository<BankStatementLine, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateStatementLineCommandHandler(IRepository<BankStatementLine, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateStatementLineCommand cmd, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(cmd.Id, ct);
        if (l is null) return ApiResponse.Fail("Statement line not found.");
        if (l.Status != BankStatementLineStatus.Unmatched)
            return ApiResponse.Fail($"Cannot edit a {l.Status} line (unmatch it first).");
        l.TransactionDate = cmd.TransactionDate;
        l.Description = cmd.Description.Trim();
        l.ReferenceNumber = string.IsNullOrWhiteSpace(cmd.ReferenceNumber) ? null : cmd.ReferenceNumber.Trim();
        l.Amount = cmd.Amount;
        l.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(l);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Statement line updated.");
    }
}

// ── Delete line (Unmatched only) ──
public sealed record DeleteStatementLineCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteStatementLineCommandHandler : IRequestHandler<DeleteStatementLineCommand, ApiResponse>
{
    private readonly IRepository<BankStatementLine, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteStatementLineCommandHandler(IRepository<BankStatementLine, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteStatementLineCommand cmd, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(cmd.Id, ct);
        if (l is null) return ApiResponse.Fail("Statement line not found.");
        if (l.Status != BankStatementLineStatus.Unmatched)
            return ApiResponse.Fail($"Cannot delete a {l.Status} line (unmatch it first).");
        _repo.Remove(l);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Statement line deleted.");
    }
}

// ── Match line to a journal line ──
public sealed record MatchStatementLineCommand(long Id, long JournalLineId) : IRequest<ApiResponse>;

internal sealed class MatchStatementLineCommandHandler : IRequestHandler<MatchStatementLineCommand, ApiResponse>
{
    private readonly IRepository<BankStatementLine, long> _repo;
    private readonly IRepository<JournalEntryLine, long> _jLineRepo;
    private readonly IRepository<BankStatement, long> _stmtRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public MatchStatementLineCommandHandler(
        IRepository<BankStatementLine, long> repo,
        IRepository<JournalEntryLine, long> jLineRepo,
        IRepository<BankStatement, long> stmtRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    { _repo = repo; _jLineRepo = jLineRepo; _stmtRepo = stmtRepo; _uow = uow; _currentUser = currentUser; _clock = clock; }

    public async Task<ApiResponse> Handle(MatchStatementLineCommand cmd, CancellationToken ct)
    {
        var line = await _repo.GetByIdAsync(cmd.Id, ct);
        if (line is null) return ApiResponse.Fail("Statement line not found.");
        if (line.Status == BankStatementLineStatus.Matched)
            return ApiResponse.Fail("Line is already matched (unmatch first to re-match).");

        var stmt = await _stmtRepo.Query()
            .Include(s => s.BankAccount)
            .FirstOrDefaultAsync(s => s.Id == line.BankStatementId, ct);
        if (stmt is null) return ApiResponse.Fail("Statement not found.");
        if (stmt.IsReconciled) return ApiResponse.Fail("Cannot match lines on a reconciled statement.");
        if (stmt.BankAccount.LedgerAccountId is null)
            return ApiResponse.Fail("Bank account is not linked to a ledger account — link it first under Master Setup.");

        var jLine = await _jLineRepo.Query()
            .Include(j => j.JournalEntry)
            .FirstOrDefaultAsync(j => j.Id == cmd.JournalLineId, ct);
        if (jLine is null) return ApiResponse.Fail("Journal line not found.");
        if (jLine.JournalEntry.Status != JournalEntryStatus.Posted)
            return ApiResponse.Fail("Can only match against posted journal lines.");
        if (jLine.AccountId != stmt.BankAccount.LedgerAccountId.Value)
            return ApiResponse.Fail("Journal line is not on this bank's ledger account.");

        // Check this journal line isn't already matched to another statement line
        var alreadyMatched = await _repo.Query()
            .AnyAsync(l => l.MatchedJournalLineId == cmd.JournalLineId && l.Id != cmd.Id, ct);
        if (alreadyMatched)
            return ApiResponse.Fail("This journal line is already matched to another statement line.");

        // Amount check: statement amount must equal (Dr − Cr) on the ledger line
        var jSignedAmount = jLine.Debit - jLine.Credit;
        if (Math.Round(line.Amount, 2) != Math.Round(jSignedAmount, 2))
            return ApiResponse.Fail($"Amount mismatch: statement {line.Amount}, journal {jSignedAmount}.");

        line.Status = BankStatementLineStatus.Matched;
        line.MatchedJournalLineId = jLine.Id;
        line.MatchedAt = _clock.UtcNow;
        line.MatchedBy = _currentUser.UserName ?? _currentUser.UserId;
        _repo.Update(line);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Line matched.");
    }
}

// ── Unmatch line (reset to Unmatched) ──
public sealed record UnmatchStatementLineCommand(long Id) : IRequest<ApiResponse>;

internal sealed class UnmatchStatementLineCommandHandler : IRequestHandler<UnmatchStatementLineCommand, ApiResponse>
{
    private readonly IRepository<BankStatementLine, long> _repo;
    private readonly IRepository<BankStatement, long> _stmtRepo;
    private readonly IUnitOfWork _uow;
    public UnmatchStatementLineCommandHandler(IRepository<BankStatementLine, long> repo,
        IRepository<BankStatement, long> stmtRepo, IUnitOfWork uow)
    { _repo = repo; _stmtRepo = stmtRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(UnmatchStatementLineCommand cmd, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(cmd.Id, ct);
        if (l is null) return ApiResponse.Fail("Statement line not found.");
        var stmt = await _stmtRepo.GetByIdAsync(l.BankStatementId, ct);
        if (stmt is { IsReconciled: true })
            return ApiResponse.Fail("Cannot unmatch lines on a reconciled statement.");
        l.Status = BankStatementLineStatus.Unmatched;
        l.MatchedJournalLineId = null;
        l.MatchedAt = null;
        l.MatchedBy = null;
        _repo.Update(l);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Line unmatched.");
    }
}

// ── Set line as Excluded (bank fee, interest etc. with no ledger counterpart) ──
public sealed record ExcludeStatementLineCommand(long Id, string? Notes) : IRequest<ApiResponse>;

internal sealed class ExcludeStatementLineCommandHandler : IRequestHandler<ExcludeStatementLineCommand, ApiResponse>
{
    private readonly IRepository<BankStatementLine, long> _repo;
    private readonly IRepository<BankStatement, long> _stmtRepo;
    private readonly IUnitOfWork _uow;
    public ExcludeStatementLineCommandHandler(IRepository<BankStatementLine, long> repo,
        IRepository<BankStatement, long> stmtRepo, IUnitOfWork uow)
    { _repo = repo; _stmtRepo = stmtRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(ExcludeStatementLineCommand cmd, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(cmd.Id, ct);
        if (l is null) return ApiResponse.Fail("Statement line not found.");
        var stmt = await _stmtRepo.GetByIdAsync(l.BankStatementId, ct);
        if (stmt is { IsReconciled: true })
            return ApiResponse.Fail("Cannot exclude lines on a reconciled statement.");
        if (l.Status == BankStatementLineStatus.Matched)
            return ApiResponse.Fail("Unmatch first, then exclude.");
        l.Status = BankStatementLineStatus.Excluded;
        if (!string.IsNullOrWhiteSpace(cmd.Notes))
            l.Notes = cmd.Notes.Trim();
        _repo.Update(l);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Line excluded.");
    }
}
