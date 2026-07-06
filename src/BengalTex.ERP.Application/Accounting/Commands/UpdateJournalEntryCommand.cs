using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

public sealed record UpdateJournalEntryCommand(
    long Id,
    DateOnly EntryDate,
    string? Reference,
    string? Narration,
    IReadOnlyList<JournalEntryLineInput> Lines
) : IRequest<ApiResponse<JournalEntryDto>>;

public sealed class UpdateJournalEntryCommandValidator : AbstractValidator<UpdateJournalEntryCommand>
{
    public UpdateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Narration).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count >= 2)
            .WithMessage("A journal voucher needs at least two lines.");
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
            .Must(x => { var d = x.Lines.Sum(l => l.Debit); return d > 0 && d == x.Lines.Sum(l => l.Credit); })
            .WithMessage("Total debits must equal total credits (and be greater than zero).")
            .When(x => x.Lines is { Count: >= 2 });
    }
}

internal sealed class UpdateJournalEntryCommandHandler
    : IRequestHandler<UpdateJournalEntryCommand, ApiResponse<JournalEntryDto>>
{
    private readonly IRepository<JournalEntry, long> _repo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly Services.IPeriodGuard _periodGuard;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateJournalEntryCommandHandler(
        IRepository<JournalEntry, long> repo,
        IRepository<Domain.Entities.Account> accountRepo,
        Services.IPeriodGuard periodGuard,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _accountRepo = accountRepo;
        _periodGuard = periodGuard;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<JournalEntryDto>> Handle(
        UpdateJournalEntryCommand cmd, CancellationToken cancellationToken)
    {
        var entry = await _repo.Query()
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == cmd.Id, cancellationToken);
        if (entry is null) return ApiResponse<JournalEntryDto>.Fail("Journal voucher not found.");
        if (entry.Status != JournalEntryStatus.Draft)
            return ApiResponse<JournalEntryDto>.Fail("Only draft journal vouchers can be edited.");

        // Phase A1 — fiscal-period guard on the (possibly changed) entry date.
        var refusal = await _periodGuard.CheckAsync(cmd.EntryDate, isManualVoucher: true, cancellationToken);
        if (refusal is not null) return ApiResponse<JournalEntryDto>.Fail(refusal);

        var accountIds = cmd.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _accountRepo.Query()
            .Where(a => accountIds.Contains(a.Id)).ToListAsync(cancellationToken);
        foreach (var id in accountIds)
        {
            var acc = accounts.FirstOrDefault(a => a.Id == id);
            if (acc is null) return ApiResponse<JournalEntryDto>.Fail($"Account {id} not found.");
            if (acc.IsGroup) return ApiResponse<JournalEntryDto>.Fail($"'{acc.Name}' is a group account — postings go to detail accounts only.");
            if (!acc.IsActive) return ApiResponse<JournalEntryDto>.Fail($"'{acc.Name}' is inactive.");
        }

        entry.EntryDate = cmd.EntryDate;
        entry.Reference = string.IsNullOrWhiteSpace(cmd.Reference) ? null : cmd.Reference.Trim();
        entry.Narration = string.IsNullOrWhiteSpace(cmd.Narration) ? null : cmd.Narration.Trim();

        entry.Lines.Clear();
        var sort = 0;
        foreach (var l in cmd.Lines)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                LineNarration = string.IsNullOrWhiteSpace(l.LineNarration) ? null : l.LineNarration.Trim(),
                CostCenterId = l.CostCenterId,   // Phase A3
                SortOrder = sort++
            });
        }

        _repo.Update(entry);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetJournalEntryByIdQuery(entry.Id), cancellationToken);
    }
}
