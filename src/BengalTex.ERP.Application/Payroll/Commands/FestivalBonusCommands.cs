using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Commands;

// ── List ──
public sealed record GetFestivalBonusesQuery(
    PagedQueryParameters Parameters,
    int? Year = null,
    string? BonusType = null,
    string? Status = null,
    int? EmployeeId = null
) : IRequest<ApiResponse<PagedResult<FestivalBonusDto>>>;

internal sealed class GetFestivalBonusesQueryHandler
    : IRequestHandler<GetFestivalBonusesQuery, ApiResponse<PagedResult<FestivalBonusDto>>>
{
    private readonly IRepository<FestivalBonus, long> _repo;
    public GetFestivalBonusesQueryHandler(IRepository<FestivalBonus, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<FestivalBonusDto>>> Handle(GetFestivalBonusesQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (request.Year.HasValue) q = q.Where(b => b.BonusYear == request.Year.Value);
        if (!string.IsNullOrEmpty(request.BonusType) && Enum.TryParse<FestivalBonusType>(request.BonusType, out var t))
            q = q.Where(b => b.BonusType == t);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<FestivalBonusStatus>(request.Status, out var s))
            q = q.Where(b => b.Status == s);
        if (request.EmployeeId.HasValue) q = q.Where(b => b.EmployeeId == request.EmployeeId.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(b => b.Code.Contains(search) ||
                             b.Employee.Code.Contains(search) ||
                             b.Employee.FullName.Contains(search));

        q = q.OrderByDescending(b => b.BonusYear).ThenBy(b => b.BonusType).ThenBy(b => b.Employee.FullName);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(b => new FestivalBonusDto(
                b.Id, b.Code, b.EmployeeId, b.Employee.Code, b.Employee.FullName,
                b.BonusYear, b.BonusType.ToString(), b.Amount,
                b.Status.ToString(), b.PaymentMethod.ToString(),
                b.PaidAt, b.PaidBy, b.Notes))
            .ToListAsync(ct);

        var result = PagedResult<FestivalBonusDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, total);
        return ApiResponse<PagedResult<FestivalBonusDto>>.Ok(result);
    }
}

// ── Bulk Create (issue to all active employees) ──
public sealed record BulkCreateFestivalBonusCommand(
    int BonusYear,
    string BonusType,
    decimal Amount,
    string? Notes
) : IRequest<ApiResponse<int>>;

public sealed class BulkCreateFestivalBonusCommandValidator : AbstractValidator<BulkCreateFestivalBonusCommand>
{
    public BulkCreateFestivalBonusCommandValidator()
    {
        RuleFor(x => x.BonusYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.BonusType).NotEmpty()
            .Must(s => Enum.TryParse<FestivalBonusType>(s, out _))
            .WithMessage("BonusType must be EidUlFitr, EidUlAzha, PohelaBoishakh, or Other.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class BulkCreateFestivalBonusCommandHandler : IRequestHandler<BulkCreateFestivalBonusCommand, ApiResponse<int>>
{
    private readonly IRepository<FestivalBonus, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public BulkCreateFestivalBonusCommandHandler(
        IRepository<FestivalBonus, long> repo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _empRepo = empRepo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<int>> Handle(BulkCreateFestivalBonusCommand cmd, CancellationToken ct)
    {
        var type = Enum.Parse<FestivalBonusType>(cmd.BonusType);
        var employees = await _empRepo.Query()
            .Where(e => e.IsActive && e.Status == EmployeeStatus.Active)
            .Select(e => e.Id).ToListAsync(ct);
        if (employees.Count == 0) return ApiResponse<int>.Fail("No active employees.");

        var existing = (await _repo.Query()
            .Where(b => b.BonusYear == cmd.BonusYear && b.BonusType == type)
            .Select(b => b.EmployeeId).ToListAsync(ct)).ToHashSet();

        var notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        var created = 0;
        foreach (var empId in employees)
        {
            if (existing.Contains(empId)) continue;
            var code = await _numbering.NextAsync("FB", null, ct);
            await _repo.AddAsync(new FestivalBonus
            {
                Code = code,
                EmployeeId = empId,
                BonusYear = cmd.BonusYear,
                BonusType = type,
                Amount = cmd.Amount,
                Status = FestivalBonusStatus.Draft,
                PaymentMethod = PaymentMethod.BankTransfer,
                Notes = notes
            }, ct);
            created++;
        }
        if (created == 0) return ApiResponse<int>.Fail("All active employees already have a bonus for this year + festival.");
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(created, $"{created} festival bonus(es) created (Draft).");
    }
}

// ── Update single Draft bonus ──
public sealed record UpdateFestivalBonusCommand(long Id, decimal Amount, string PaymentMethod, string? Notes) : IRequest<ApiResponse>;

public sealed class UpdateFestivalBonusCommandValidator : AbstractValidator<UpdateFestivalBonusCommand>
{
    public UpdateFestivalBonusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty().Must(s => Enum.TryParse<PaymentMethod>(s, out _));
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateFestivalBonusCommandHandler : IRequestHandler<UpdateFestivalBonusCommand, ApiResponse>
{
    private readonly IRepository<FestivalBonus, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateFestivalBonusCommandHandler(IRepository<FestivalBonus, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateFestivalBonusCommand cmd, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) return ApiResponse.Fail("Bonus not found.");
        if (b.Status != FestivalBonusStatus.Draft) return ApiResponse.Fail($"Cannot edit a {b.Status} bonus.");
        b.Amount = cmd.Amount;
        b.PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        b.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(b);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Bonus updated.");
    }
}

// ── Delete (Draft only) ──
public sealed record DeleteFestivalBonusCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteFestivalBonusCommandHandler : IRequestHandler<DeleteFestivalBonusCommand, ApiResponse>
{
    private readonly IRepository<FestivalBonus, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteFestivalBonusCommandHandler(IRepository<FestivalBonus, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteFestivalBonusCommand cmd, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) return ApiResponse.Fail("Bonus not found.");
        if (b.Status != FestivalBonusStatus.Draft) return ApiResponse.Fail($"Cannot delete a {b.Status} bonus (cancel via Pay-status flow instead).");
        _repo.Remove(b);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Bonus deleted.");
    }
}

// ── Pay (auto-journal: Dr Salary Expense / Cr Cash|Bank) ──
public sealed record PayFestivalBonusCommand(long Id) : IRequest<ApiResponse>;

internal sealed class PayFestivalBonusCommandHandler : IRequestHandler<PayFestivalBonusCommand, ApiResponse>
{
    private readonly IRepository<FestivalBonus, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;

    public PayFestivalBonusCommandHandler(
        IRepository<FestivalBonus, long> repo, IUnitOfWork uow,
        ICurrentUserService currentUser, IJournalPostingService journal)
    { _repo = repo; _uow = uow; _currentUser = currentUser; _journal = journal; }

    public async Task<ApiResponse> Handle(PayFestivalBonusCommand cmd, CancellationToken ct)
    {
        var b = await _repo.Query().Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (b is null) return ApiResponse.Fail("Bonus not found.");
        if (b.Status != FestivalBonusStatus.Draft) return ApiResponse.Fail($"Cannot pay a {b.Status} bonus.");

        b.Status = FestivalBonusStatus.Paid;
        b.PaidAt = DateTimeOffset.UtcNow;
        b.PaidBy = _currentUser.UserName;
        _repo.Update(b);

        if (b.Amount > 0m)
        {
            var cashAccount = b.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
            var payDate = DateOnly.FromDateTime(b.PaidAt.Value.UtcDateTime);
            await _journal.PostAsync(
                payDate,
                $"Festival bonus {b.BonusType} {b.BonusYear} — {b.Employee.FullName} ({b.Employee.Code})",
                "FestivalBonus", b.Id, b.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.SalaryExpense, b.Amount, 0m),
                    new JournalPostingLine(cashAccount, 0m, b.Amount),
                }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Bonus {b.Code} marked paid.");
    }
}
