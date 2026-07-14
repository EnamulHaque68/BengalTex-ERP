using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Statutory;

// ═══════════════════════════ Shared mapping ═══════════════════════════

internal static class StatutoryAccounts
{
    /// <summary>The payable account a withholding type is held in.</summary>
    public static string CodeFor(StatutoryTaxType type) => type switch
    {
        StatutoryTaxType.Ait => LedgerAccounts.AitPayable,             // 2160
        StatutoryTaxType.Vds => LedgerAccounts.VdsPayable,             // 2170
        StatutoryTaxType.ProvidentFund => LedgerAccounts.ProvidentFundPayable, // 2135
        _ => LedgerAccounts.AitPayable
    };

    public static string Label(StatutoryTaxType type) => type switch
    {
        StatutoryTaxType.Ait => "AIT (Income Tax at Source)",
        StatutoryTaxType.Vds => "VDS (VAT Deducted at Source)",
        StatutoryTaxType.ProvidentFund => "Provident Fund",
        _ => type.ToString()
    };
}

// ═══════════════════════════ Outstanding liabilities ═══════════════════════════

public sealed record StatutoryLiabilityDto(
    string TaxType, string Label, string AccountCode, decimal Outstanding);

public sealed record StatutoryLiabilitiesDto(DateOnly AsOfDate, IReadOnlyList<StatutoryLiabilityDto> Items);

/// <summary>Phase A5b — outstanding AIT / VDS / PF payable balances (Cr − Dr) as of a date, from posted GL.</summary>
public sealed record GetStatutoryLiabilitiesQuery(DateOnly? AsOfDate = null)
    : IRequest<ApiResponse<StatutoryLiabilitiesDto>>;

internal sealed class GetStatutoryLiabilitiesQueryHandler
    : IRequestHandler<GetStatutoryLiabilitiesQuery, ApiResponse<StatutoryLiabilitiesDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    public GetStatutoryLiabilitiesQueryHandler(IRepository<JournalEntryLine, long> lineRepo) => _lineRepo = lineRepo;

    public async Task<ApiResponse<StatutoryLiabilitiesDto>> Handle(GetStatutoryLiabilitiesQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var items = new List<StatutoryLiabilityDto>();

        foreach (var type in new[] { StatutoryTaxType.Ait, StatutoryTaxType.Vds, StatutoryTaxType.ProvidentFund })
        {
            var code = StatutoryAccounts.CodeFor(type);
            // Liability normal balance = credit, so outstanding = Σ(Credit − Debit).
            var bal = await _lineRepo.Query().AsNoTracking()
                .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                         && l.JournalEntry.EntryDate <= asOf
                         && l.Account.Code == code)
                .Select(l => l.Credit - l.Debit).SumAsync(ct);
            items.Add(new StatutoryLiabilityDto(type.ToString(), StatutoryAccounts.Label(type), code, Math.Round(bal, 2)));
        }

        return ApiResponse<StatutoryLiabilitiesDto>.Ok(new StatutoryLiabilitiesDto(asOf, items));
    }
}

// ═══════════════════════════ Remittance register ═══════════════════════════

public sealed record StatutoryRemittanceDto(
    long Id, string Code, string TaxType, int PeriodYear, int PeriodMonth,
    decimal Amount, DateOnly RemittanceDate, string PaymentMethod, string? ChallanNo, string? Notes);

public sealed record GetStatutoryRemittancesQuery(string? TaxType = null)
    : IRequest<ApiResponse<IReadOnlyList<StatutoryRemittanceDto>>>;

internal sealed class GetStatutoryRemittancesQueryHandler
    : IRequestHandler<GetStatutoryRemittancesQuery, ApiResponse<IReadOnlyList<StatutoryRemittanceDto>>>
{
    private readonly IRepository<StatutoryRemittance, long> _repo;
    public GetStatutoryRemittancesQueryHandler(IRepository<StatutoryRemittance, long> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<StatutoryRemittanceDto>>> Handle(
        GetStatutoryRemittancesQuery q, CancellationToken ct)
    {
        var query = _repo.Query().AsNoTracking();
        if (!string.IsNullOrEmpty(q.TaxType) && Enum.TryParse<StatutoryTaxType>(q.TaxType, out var t))
            query = query.Where(r => r.TaxType == t);

        var rows = await query
            .OrderByDescending(r => r.RemittanceDate).ThenByDescending(r => r.Id)
            .Select(r => new StatutoryRemittanceDto(
                r.Id, r.Code, r.TaxType.ToString(), r.PeriodYear, r.PeriodMonth,
                r.Amount, r.RemittanceDate, r.PaymentMethod.ToString(), r.ChallanNo, r.Notes))
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<StatutoryRemittanceDto>>.Ok(rows);
    }
}

// ═══════════════════════════ Post a remittance ═══════════════════════════

/// <summary>
/// Phase A5b — remit a withheld statutory liability to the government / fund on a challan:
/// posts <c>Dr 2160|2170|2135 / Cr Cash|Bank</c> and records the challan in the register.
/// </summary>
public sealed record PostStatutoryRemittanceCommand(
    string TaxType, int PeriodYear, int PeriodMonth, decimal Amount, DateOnly RemittanceDate,
    string PaymentMethod, string? ChallanNo, string? Notes) : IRequest<ApiResponse<long>>;

public sealed class PostStatutoryRemittanceCommandValidator : AbstractValidator<PostStatutoryRemittanceCommand>
{
    public PostStatutoryRemittanceCommandValidator()
    {
        RuleFor(x => x.TaxType).NotEmpty()
            .Must(t => Enum.TryParse<StatutoryTaxType>(t, out _)).WithMessage("TaxType must be Ait, Vds, or ProvidentFund.");
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.RemittanceDate).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.ChallanNo).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class PostStatutoryRemittanceCommandHandler
    : IRequestHandler<PostStatutoryRemittanceCommand, ApiResponse<long>>
{
    private readonly IRepository<StatutoryRemittance, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IJournalPostingService _journal;

    public PostStatutoryRemittanceCommandHandler(
        IRepository<StatutoryRemittance, long> repo, IUnitOfWork uow,
        INumberingService numbering, IJournalPostingService journal)
    {
        _repo = repo; _uow = uow; _numbering = numbering; _journal = journal;
    }

    public async Task<ApiResponse<long>> Handle(PostStatutoryRemittanceCommand cmd, CancellationToken ct)
    {
        var type = Enum.Parse<StatutoryTaxType>(cmd.TaxType);
        var method = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        var amount = Math.Round(cmd.Amount, 2, MidpointRounding.AwayFromZero);

        var code = await _numbering.NextAsync("SR", null, ct);
        var entity = new StatutoryRemittance
        {
            Code = code,
            TaxType = type,
            PeriodYear = cmd.PeriodYear,
            PeriodMonth = cmd.PeriodMonth,
            Amount = amount,
            RemittanceDate = cmd.RemittanceDate,
            PaymentMethod = method,
            ChallanNo = string.IsNullOrWhiteSpace(cmd.ChallanNo) ? null : cmd.ChallanNo.Trim(),
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);   // persist so the Id exists for the journal source link

        var cashAccount = method == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
        await _journal.PostAsync(
            cmd.RemittanceDate,
            $"Statutory remittance {code} — {StatutoryAccounts.Label(type)} {cmd.PeriodYear}-{cmd.PeriodMonth:D2}",
            "StatutoryRemittance", entity.Id, code,
            new[]
            {
                new JournalPostingLine(StatutoryAccounts.CodeFor(type), amount, 0m),
                new JournalPostingLine(cashAccount, 0m, amount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(entity.Id, $"{StatutoryAccounts.Label(type)} remitted.");
    }
}
