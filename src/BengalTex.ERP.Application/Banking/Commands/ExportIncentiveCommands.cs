using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Banking.Commands;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record ExportIncentiveClaimDto(
    long Id, string Code, long? CustomerInvoiceId, string? CustomerInvoiceCode,
    string? ExportReference, decimal IncentiveRate, decimal Amount, DateOnly ClaimDate,
    string Status, DateOnly? ReceivedDate, string? ReceivedMethod, string? BankReference, string? Notes);

public sealed record ExportIncentiveListDto(
    IReadOnlyList<ExportIncentiveClaimDto> Items, decimal OutstandingReceivable);

// ═══════════════════════════ Query ═══════════════════════════

public sealed record GetExportIncentiveClaimsQuery(string? Status = null)
    : IRequest<ApiResponse<ExportIncentiveListDto>>;

internal sealed class GetExportIncentiveClaimsQueryHandler
    : IRequestHandler<GetExportIncentiveClaimsQuery, ApiResponse<ExportIncentiveListDto>>
{
    private readonly IRepository<ExportIncentiveClaim, long> _repo;
    public GetExportIncentiveClaimsQueryHandler(IRepository<ExportIncentiveClaim, long> repo) => _repo = repo;

    public async Task<ApiResponse<ExportIncentiveListDto>> Handle(GetExportIncentiveClaimsQuery q, CancellationToken ct)
    {
        var query = _repo.Query().AsNoTracking();
        if (!string.IsNullOrEmpty(q.Status) && Enum.TryParse<IncentiveClaimStatus>(q.Status, out var s))
            query = query.Where(c => c.Status == s);

        var items = await query
            .OrderByDescending(c => c.ClaimDate).ThenByDescending(c => c.Id)
            .Select(c => new ExportIncentiveClaimDto(
                c.Id, c.Code, c.CustomerInvoiceId, c.CustomerInvoice != null ? c.CustomerInvoice.Code : null,
                c.ExportReference, c.IncentiveRate, c.Amount, c.ClaimDate,
                c.Status.ToString(), c.ReceivedDate,
                c.ReceivedMethod != null ? c.ReceivedMethod.ToString() : null, c.BankReference, c.Notes))
            .ToListAsync(ct);

        // Outstanding = accrued but not yet received (the 1186 receivable still open).
        var outstanding = await _repo.Query().AsNoTracking()
            .Where(c => c.Status == IncentiveClaimStatus.Accrued).SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;

        return ApiResponse<ExportIncentiveListDto>.Ok(new ExportIncentiveListDto(items, Math.Round(outstanding, 2)));
    }
}

// ═══════════════════════════ Create (accrue) ═══════════════════════════

public sealed record CreateExportIncentiveClaimCommand(
    long? CustomerInvoiceId, string? ExportReference, decimal IncentiveRate, decimal Amount,
    DateOnly ClaimDate, string? Notes) : IRequest<ApiResponse<long>>;

public sealed class CreateExportIncentiveClaimCommandValidator : AbstractValidator<CreateExportIncentiveClaimCommand>
{
    public CreateExportIncentiveClaimCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.IncentiveRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClaimDate).NotEmpty();
        RuleFor(x => x.ExportReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateExportIncentiveClaimCommandHandler
    : IRequestHandler<CreateExportIncentiveClaimCommand, ApiResponse<long>>
{
    private readonly IRepository<ExportIncentiveClaim, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IJournalPostingService _journal;

    public CreateExportIncentiveClaimCommandHandler(
        IRepository<ExportIncentiveClaim, long> repo, IUnitOfWork uow,
        INumberingService numbering, IJournalPostingService journal)
    {
        _repo = repo; _uow = uow; _numbering = numbering; _journal = journal;
    }

    public async Task<ApiResponse<long>> Handle(CreateExportIncentiveClaimCommand cmd, CancellationToken ct)
    {
        var amount = Math.Round(cmd.Amount, 2, MidpointRounding.AwayFromZero);
        var code = await _numbering.NextAsync("EI", null, ct);
        var entity = new ExportIncentiveClaim
        {
            Code = code,
            CustomerInvoiceId = cmd.CustomerInvoiceId,
            ExportReference = string.IsNullOrWhiteSpace(cmd.ExportReference) ? null : cmd.ExportReference.Trim(),
            IncentiveRate = cmd.IncentiveRate,
            Amount = amount,
            ClaimDate = cmd.ClaimDate,
            Status = IncentiveClaimStatus.Accrued,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // Accrue the incentive income against a receivable from the government.
        await _journal.PostAsync(
            cmd.ClaimDate, $"Export incentive accrued {code}", "ExportIncentiveClaim", entity.Id, code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.ExportIncentiveReceivable, amount, 0m),
                new JournalPostingLine(LedgerAccounts.ExportIncentiveIncome, 0m, amount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(entity.Id, "Export incentive accrued.");
    }
}

// ═══════════════════════════ Mark received ═══════════════════════════

public sealed record MarkIncentiveReceivedCommand(
    long Id, DateOnly ReceivedDate, string PaymentMethod, string? BankReference) : IRequest<ApiResponse>;

public sealed class MarkIncentiveReceivedCommandValidator : AbstractValidator<MarkIncentiveReceivedCommand>
{
    public MarkIncentiveReceivedCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ReceivedDate).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.BankReference).MaximumLength(100);
    }
}

internal sealed class MarkIncentiveReceivedCommandHandler : IRequestHandler<MarkIncentiveReceivedCommand, ApiResponse>
{
    private readonly IRepository<ExportIncentiveClaim, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public MarkIncentiveReceivedCommandHandler(
        IRepository<ExportIncentiveClaim, long> repo, IUnitOfWork uow, IJournalPostingService journal)
    {
        _repo = repo; _uow = uow; _journal = journal;
    }

    public async Task<ApiResponse> Handle(MarkIncentiveReceivedCommand cmd, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(cmd.Id, ct);
        if (c is null) return ApiResponse.Fail("Incentive claim not found.");
        if (c.Status != IncentiveClaimStatus.Accrued)
            return ApiResponse.Fail($"Only an accrued claim can be marked received (this one is {c.Status}).");

        var method = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        c.Status = IncentiveClaimStatus.Received;
        c.ReceivedDate = cmd.ReceivedDate;
        c.ReceivedMethod = method;
        c.BankReference = string.IsNullOrWhiteSpace(cmd.BankReference) ? null : cmd.BankReference.Trim();
        _repo.Update(c);

        var cash = method == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
        await _journal.PostAsync(
            cmd.ReceivedDate, $"Export incentive received {c.Code}", "ExportIncentiveClaim", c.Id, c.Code,
            new[]
            {
                new JournalPostingLine(cash, c.Amount, 0m),
                new JournalPostingLine(LedgerAccounts.ExportIncentiveReceivable, 0m, c.Amount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Export incentive received.");
    }
}

// ═══════════════════════════ Cancel (reverse accrual) ═══════════════════════════

public sealed record CancelExportIncentiveClaimCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CancelExportIncentiveClaimCommandHandler
    : IRequestHandler<CancelExportIncentiveClaimCommand, ApiResponse>
{
    private readonly IRepository<ExportIncentiveClaim, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public CancelExportIncentiveClaimCommandHandler(
        IRepository<ExportIncentiveClaim, long> repo, IUnitOfWork uow, IJournalPostingService journal)
    {
        _repo = repo; _uow = uow; _journal = journal;
    }

    public async Task<ApiResponse> Handle(CancelExportIncentiveClaimCommand cmd, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(cmd.Id, ct);
        if (c is null) return ApiResponse.Fail("Incentive claim not found.");
        if (c.Status != IncentiveClaimStatus.Accrued)
            return ApiResponse.Fail($"Only an accrued claim can be cancelled (this one is {c.Status}).");

        c.Status = IncentiveClaimStatus.Cancelled;
        _repo.Update(c);

        // Reverse the accrual (Dr income / Cr receivable).
        await _journal.PostAsync(
            DateOnly.FromDateTime(DateTime.UtcNow), $"Export incentive cancelled {c.Code}",
            "ExportIncentiveClaim", c.Id, c.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.ExportIncentiveIncome, c.Amount, 0m),
                new JournalPostingLine(LedgerAccounts.ExportIncentiveReceivable, 0m, c.Amount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Export incentive claim cancelled.");
    }
}
