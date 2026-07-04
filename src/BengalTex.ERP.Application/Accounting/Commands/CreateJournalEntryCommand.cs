using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

/// <summary>One leg of a manual journal voucher — a debit OR a credit to one account.</summary>
public sealed record JournalEntryLineInput(
    int AccountId,
    decimal Debit,
    decimal Credit,
    string? LineNarration);

public sealed record CreateJournalEntryCommand(
    DateOnly EntryDate,
    string? Reference,
    string? Narration,
    IReadOnlyList<JournalEntryLineInput> Lines
) : IRequest<ApiResponse<JournalEntryDto>>;

public sealed class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Narration).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A journal voucher needs at least two lines.")
            .Must(l => l.Count >= 2).WithMessage("A journal voucher needs at least two lines.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).GreaterThan(0);
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l)
                .Must(l => (l.Debit > 0) ^ (l.Credit > 0))
                .WithMessage("Each line must have exactly one of Debit or Credit greater than zero.");
        });
        RuleFor(x => x)
            .Must(BeBalanced)
            .WithMessage("Total debits must equal total credits (and be greater than zero).")
            .When(x => x.Lines is { Count: >= 2 });
    }

    private static bool BeBalanced(CreateJournalEntryCommand c)
    {
        var debit = c.Lines.Sum(l => l.Debit);
        var credit = c.Lines.Sum(l => l.Credit);
        return debit > 0 && debit == credit;
    }
}

internal sealed class CreateJournalEntryCommandHandler
    : IRequestHandler<CreateJournalEntryCommand, ApiResponse<JournalEntryDto>>
{
    private readonly IRepository<JournalEntry, long> _repo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateJournalEntryCommandHandler(
        IRepository<JournalEntry, long> repo,
        IRepository<Domain.Entities.Account> accountRepo,
        IPeriodGuard periodGuard,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _accountRepo = accountRepo;
        _periodGuard = periodGuard;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<JournalEntryDto>> Handle(
        CreateJournalEntryCommand cmd, CancellationToken cancellationToken)
    {
        // Phase A1 — fiscal-period guard (manual voucher path).
        var refusal = await _periodGuard.CheckAsync(cmd.EntryDate, isManualVoucher: true, cancellationToken);
        if (refusal is not null) return ApiResponse<JournalEntryDto>.Fail(refusal);

        var accountIds = cmd.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _accountRepo.Query()
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        foreach (var id in accountIds)
        {
            var acc = accounts.FirstOrDefault(a => a.Id == id);
            if (acc is null) return ApiResponse<JournalEntryDto>.Fail($"Account {id} not found.");
            if (acc.IsGroup) return ApiResponse<JournalEntryDto>.Fail($"'{acc.Name}' is a group account — postings go to detail accounts only.");
            if (!acc.IsActive) return ApiResponse<JournalEntryDto>.Fail($"'{acc.Name}' is inactive.");
        }

        var code = await _numbering.NextAsync("JV", null, cancellationToken);

        var entity = new JournalEntry
        {
            Code = code,
            EntryDate = cmd.EntryDate,
            Reference = string.IsNullOrWhiteSpace(cmd.Reference) ? null : cmd.Reference.Trim(),
            Narration = string.IsNullOrWhiteSpace(cmd.Narration) ? null : cmd.Narration.Trim(),
            Status = JournalEntryStatus.Draft,
            Lines = cmd.Lines.Select((l, i) => new JournalEntryLine
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                LineNarration = string.IsNullOrWhiteSpace(l.LineNarration) ? null : l.LineNarration.Trim(),
                SortOrder = i
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetJournalEntryByIdQuery(entity.Id), cancellationToken);
    }
}
