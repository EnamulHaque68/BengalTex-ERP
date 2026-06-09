using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.BankReconciliation.Commands;

/// <summary>
/// One parsed transaction row from the imported CSV. Amount is SIGNED:
/// positive = inflow (deposit/credit on the bank's statement), negative = outflow (debit).
/// The CSV parser lives on the frontend (small file, simple format) — backend just
/// validates + persists the structured rows.
/// </summary>
public sealed record ImportBankStatementLineInput(
    DateOnly TransactionDate,
    string Description,
    string? ReferenceNumber,
    decimal Amount);

/// <summary>
/// Bulk-imports a <see cref="BankStatement"/> with all its <see cref="BankStatementLine"/>s
/// in one atomic SaveChanges. Skips creating the statement if no lines (empty CSV is
/// rejected). <see cref="OpeningBalance"/> auto-defaults to the prior statement's closing
/// balance (looked up by latest BankStatement on this account with PeriodToDate &lt; this
/// statement's PeriodFromDate) when the caller passes 0 — but caller can override.
/// <see cref="ClosingBalance"/> auto-computes as OpeningBalance + Σ amounts when caller
/// passes 0 (typical use); caller can override to manual entry if the CSV is missing
/// the bank's running balance.
/// </summary>
public sealed record ImportBankStatementCommand(
    int BankAccountId,
    DateOnly StatementDate,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    decimal OpeningBalance,        // 0 → auto-derive from prior statement
    decimal ClosingBalance,        // 0 → auto-compute = opening + Σ amounts
    string? Notes,
    IReadOnlyList<ImportBankStatementLineInput> Lines
) : IRequest<ApiResponse<long>>;

public sealed class ImportBankStatementCommandValidator : AbstractValidator<ImportBankStatementCommand>
{
    public ImportBankStatementCommandValidator()
    {
        RuleFor(x => x.BankAccountId).GreaterThan(0);
        RuleFor(x => x.StatementDate).NotEmpty();
        RuleFor(x => x.PeriodFromDate).NotEmpty();
        RuleFor(x => x.PeriodToDate).GreaterThanOrEqualTo(x => x.PeriodFromDate);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Import file has no transaction rows.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(500);
            line.RuleFor(l => l.ReferenceNumber).MaximumLength(100);
            line.RuleFor(l => l.Amount).NotEqual(0m)
                .WithMessage("Amount must be non-zero (positive = inflow, negative = outflow).");
        });
    }
}

internal sealed class ImportBankStatementCommandHandler
    : IRequestHandler<ImportBankStatementCommand, ApiResponse<long>>
{
    private readonly IRepository<BankStatement, long> _repo;
    private readonly IRepository<BankAccount> _bankRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public ImportBankStatementCommandHandler(
        IRepository<BankStatement, long> repo,
        IRepository<BankAccount> bankRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _bankRepo = bankRepo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(ImportBankStatementCommand cmd, CancellationToken ct)
    {
        if (!await _bankRepo.Query().AnyAsync(b => b.Id == cmd.BankAccountId && b.IsActive, ct))
            return ApiResponse<long>.Fail("Bank account not found or inactive.");

        // Auto-derive opening balance from prior statement when caller passes 0
        var opening = cmd.OpeningBalance;
        if (opening == 0m)
        {
            var prior = await _repo.Query()
                .Where(s => s.BankAccountId == cmd.BankAccountId && s.PeriodToDate < cmd.PeriodFromDate)
                .OrderByDescending(s => s.PeriodToDate)
                .FirstOrDefaultAsync(ct);
            if (prior is not null) opening = prior.ClosingBalance;
        }

        var sumAmounts = cmd.Lines.Sum(l => l.Amount);
        var closing = cmd.ClosingBalance == 0m ? opening + sumAmounts : cmd.ClosingBalance;

        var code = await _numbering.NextAsync("BST", null, ct);
        var statement = new BankStatement
        {
            Code = code,
            BankAccountId = cmd.BankAccountId,
            StatementDate = cmd.StatementDate,
            PeriodFromDate = cmd.PeriodFromDate,
            PeriodToDate = cmd.PeriodToDate,
            OpeningBalance = opening,
            ClosingBalance = closing,
            IsReconciled = false,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            Lines = cmd.Lines.Select(l => new BankStatementLine
            {
                TransactionDate = l.TransactionDate,
                Description = l.Description.Trim(),
                ReferenceNumber = string.IsNullOrWhiteSpace(l.ReferenceNumber) ? null : l.ReferenceNumber.Trim(),
                Amount = l.Amount,
                Status = BankStatementLineStatus.Unmatched
            }).ToList()
        };

        await _repo.AddAsync(statement, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(statement.Id,
            $"Bank statement {statement.Code} imported with {cmd.Lines.Count} transaction(s).");
    }
}
