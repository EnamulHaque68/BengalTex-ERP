using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

// ═══════════════════════════ Contra (fund transfer) voucher — Phase A1 D4 ═══════════════════════════

/// <summary>
/// Moves funds between two cash/bank accounts — bank→bank transfer, cash deposit, cash
/// withdrawal. The one money movement that previously had no document. Posts immediately:
/// Dr destination / Cr source, VoucherType=Contra, series CV.
/// </summary>
public sealed record CreateContraVoucherCommand(
    DateOnly EntryDate,
    int FromAccountId,
    int ToAccountId,
    decimal Amount,
    string? Reference,
    string? Notes
) : IRequest<ApiResponse<JournalEntryDto>>;

public sealed class CreateContraVoucherCommandValidator : AbstractValidator<CreateContraVoucherCommand>
{
    public CreateContraVoucherCommandValidator()
    {
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.FromAccountId).GreaterThan(0);
        RuleFor(x => x.ToAccountId).GreaterThan(0);
        RuleFor(x => x).Must(x => x.FromAccountId != x.ToAccountId)
            .WithMessage("Source and destination accounts must differ.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateContraVoucherCommandHandler
    : IRequestHandler<CreateContraVoucherCommand, ApiResponse<JournalEntryDto>>
{
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CreateContraVoucherCommandHandler(
        IRepository<JournalEntry, long> journalRepo,
        IRepository<Domain.Entities.Account> accountRepo,
        IPeriodGuard periodGuard,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _journalRepo = journalRepo;
        _accountRepo = accountRepo;
        _periodGuard = periodGuard;
        _numbering = numbering;
        _currentUser = currentUser;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<JournalEntryDto>> Handle(
        CreateContraVoucherCommand cmd, CancellationToken ct)
    {
        var refusal = await _periodGuard.CheckAsync(cmd.EntryDate, isManualVoucher: true, ct);
        if (refusal is not null) return ApiResponse<JournalEntryDto>.Fail(refusal);

        var from = await ValidateCashBankAsync(cmd.FromAccountId, "source", ct);
        if (from.Error is not null) return ApiResponse<JournalEntryDto>.Fail(from.Error);
        var to = await ValidateCashBankAsync(cmd.ToAccountId, "destination", ct);
        if (to.Error is not null) return ApiResponse<JournalEntryDto>.Fail(to.Error);

        var amount = Math.Round(cmd.Amount, 2, MidpointRounding.AwayFromZero);
        var entry = new JournalEntry
        {
            Code = await _numbering.NextAsync("CV", null, ct),
            EntryDate = cmd.EntryDate,
            Reference = string.IsNullOrWhiteSpace(cmd.Reference) ? null : cmd.Reference.Trim(),
            Narration = string.IsNullOrWhiteSpace(cmd.Notes)
                ? $"Fund transfer: {from.Account!.Name} → {to.Account!.Name}"
                : cmd.Notes.Trim(),
            Status = JournalEntryStatus.Posted,
            VoucherType = VoucherType.Contra,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(cmd.EntryDate, ct),
            PostedAt = DateTimeOffset.UtcNow,
            PostedBy = _currentUser.UserName ?? "system",
            Lines =
            {
                new JournalEntryLine { AccountId = to.Account!.Id, Debit = amount, Credit = 0m, SortOrder = 0 },
                new JournalEntryLine { AccountId = from.Account!.Id, Debit = 0m, Credit = amount, SortOrder = 1 }
            }
        };

        await _journalRepo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetJournalEntryByIdQuery(entry.Id), ct);
    }

    /// <summary>The account must be postable, active, and inside the Cash (1110) / Bank (1120) family.</summary>
    private async Task<(Domain.Entities.Account? Account, string? Error)> ValidateCashBankAsync(
        int accountId, string role, CancellationToken ct)
    {
        var acc = await _accountRepo.Query()
            .Include(a => a.ParentAccount!).ThenInclude(a => a.ParentAccount)
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);
        if (acc is null) return (null, $"The {role} account was not found.");
        if (acc.IsGroup) return (null, $"'{acc.Name}' is a group account — pick a postable cash/bank account.");
        if (!acc.IsActive) return (null, $"'{acc.Name}' is inactive.");

        // Walk self + ancestors looking for the seeded Cash/Bank family codes.
        var node = acc;
        var hops = 0;
        while (node is not null && hops++ < 6)
        {
            if (node.Code is Accounting.LedgerAccounts.Cash or Accounting.LedgerAccounts.Bank)
                return (acc, null);
            node = node.ParentAccount;
        }
        return (null, $"'{acc.Name}' is not a cash/bank account — contra vouchers move funds between cash and bank only.");
    }
}

// ═══════════════════════════ Journal reversal — Phase A1 D7 ═══════════════════════════

/// <summary>
/// One-click reversal of a POSTED voucher: creates a mirror entry (Dr↔Cr swapped) linked via
/// <c>ReversedEntryId</c>, with a mandatory reason. A voucher can be reversed only once.
/// </summary>
public sealed record ReverseJournalEntryCommand(
    long Id,
    string Reason,
    DateOnly? ReversalDate = null
) : IRequest<ApiResponse<JournalEntryDto>>;

public sealed class ReverseJournalEntryCommandValidator : AbstractValidator<ReverseJournalEntryCommand>
{
    public ReverseJournalEntryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reversal reason is required.").MaximumLength(500);
    }
}

internal sealed class ReverseJournalEntryCommandHandler
    : IRequestHandler<ReverseJournalEntryCommand, ApiResponse<JournalEntryDto>>
{
    private readonly IRepository<JournalEntry, long> _repo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ReverseJournalEntryCommandHandler(
        IRepository<JournalEntry, long> repo,
        IPeriodGuard periodGuard,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _periodGuard = periodGuard;
        _numbering = numbering;
        _currentUser = currentUser;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<JournalEntryDto>> Handle(
        ReverseJournalEntryCommand cmd, CancellationToken ct)
    {
        var original = await _repo.Query()
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == cmd.Id, ct);
        if (original is null) return ApiResponse<JournalEntryDto>.Fail("Journal voucher not found.");
        if (original.Status != JournalEntryStatus.Posted)
            return ApiResponse<JournalEntryDto>.Fail("Only posted vouchers can be reversed.");

        if (await _repo.AnyAsync(j => j.ReversedEntryId == original.Id, ct))
            return ApiResponse<JournalEntryDto>.Fail(
                $"{original.Code} has already been reversed — a voucher can be reversed only once.");

        var date = cmd.ReversalDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var refusal = await _periodGuard.CheckAsync(date, isManualVoucher: true, ct);
        if (refusal is not null) return ApiResponse<JournalEntryDto>.Fail(refusal);

        var reversal = new JournalEntry
        {
            Code = await _numbering.NextAsync(
                Infrastructure_SeriesShim.SeriesFor(original.VoucherType), null, ct),
            EntryDate = date,
            Reference = original.Code,
            Narration = $"Reversal of {original.Code}: {cmd.Reason.Trim()}",
            Status = JournalEntryStatus.Posted,
            VoucherType = original.VoucherType,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(date, ct),
            SourceType = "Reversal",
            SourceId = original.Id,
            SourceCode = original.Code,
            ReversedEntryId = original.Id,
            ReversalReason = cmd.Reason.Trim(),
            PostedAt = DateTimeOffset.UtcNow,
            PostedBy = _currentUser.UserName ?? "system",
            Lines = original.Lines.OrderBy(l => l.SortOrder).Select((l, i) => new JournalEntryLine
            {
                AccountId = l.AccountId,
                Debit = l.Credit,
                Credit = l.Debit,
                LineNarration = l.LineNarration,
                SortOrder = i
            }).ToList()
        };

        await _repo.AddAsync(reversal, ct);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetJournalEntryByIdQuery(reversal.Id), ct);
    }
}

/// <summary>Voucher-type → numbering-series map (mirrors the posting engine's table).</summary>
internal static class Infrastructure_SeriesShim
{
    public static string SeriesFor(VoucherType type) => type switch
    {
        VoucherType.Receipt => "RV",
        VoucherType.Payment => "PV",
        VoucherType.Contra => "CV",
        VoucherType.Opening => "OB",
        VoucherType.Closing => "CL",
        _ => "JV"
    };
}
