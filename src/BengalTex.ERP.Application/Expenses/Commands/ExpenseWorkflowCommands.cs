using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Expenses.Dtos;
using BengalTex.ERP.Application.Expenses.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Expenses.Commands;

/// <summary>
/// Records + pays an expense (Dr expense account / Cr Cash|Bank) and sets it Approved.
/// INTERNAL — invoked by <see cref="SubmitExpenseForApprovalCommand"/> on the auto-approve
/// path and by the Approvals inbox decision on sign-off; not exposed directly so every
/// expense goes through the threshold gate. Accepts Draft (auto) or PendingApproval (signed off).
/// </summary>
public sealed record ApproveExpenseCommand(long Id) : IRequest<ApiResponse<ExpenseDto>>;

internal sealed class ApproveExpenseCommandHandler : IRequestHandler<ApproveExpenseCommand, ApiResponse<ExpenseDto>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    private readonly IRepository<ExpenseCategory> _categoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ApproveExpenseCommandHandler(
        IRepository<Domain.Entities.Expense, long> repo,
        IRepository<ExpenseCategory> categoryRepo,
        IUnitOfWork uow,
        IJournalPostingService journal,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _uow = uow;
        _journal = journal;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(ApproveExpenseCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<ExpenseDto>.Fail("Expense not found.");
        if (e.Status != ExpenseStatus.Draft && e.Status != ExpenseStatus.PendingApproval)
            return ApiResponse<ExpenseDto>.Fail("Only draft or pending-approval expenses can be approved.");

        var category = await _categoryRepo.Query()
            .Include(c => c.LedgerAccount)
            .FirstOrDefaultAsync(c => c.Id == e.ExpenseCategoryId, ct);
        var expenseAccount = category?.LedgerAccount?.Code ?? LedgerAccounts.AdministrativeExpense;
        var cashAccount = e.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;

        e.Status = ExpenseStatus.Approved;
        e.ApprovedAt = DateTimeOffset.UtcNow;
        e.ApprovedBy = _currentUser.UserName;
        _repo.Update(e);

        await _journal.PostAsync(
            e.ExpenseDate, $"Expense {e.Code}" + (e.Payee is null ? "" : $" — {e.Payee}"),
            "Expense", e.Id, e.Code,
            new[]
            {
                new JournalPostingLine(expenseAccount, e.Amount, 0m),
                new JournalPostingLine(cashAccount, 0m, e.Amount),
            }, ct);

        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetExpenseByIdQuery(e.Id), ct);
    }
}

/// <summary>
/// Submits a draft expense into the approval workflow. Within the configured threshold it is
/// auto-approved (records + pays immediately via <see cref="ApproveExpenseCommand"/>); above
/// the threshold it goes to PendingApproval for sign-off via the Approvals inbox. This is the
/// single entry point for "approving" an expense — the threshold decides whether sign-off is needed.
/// </summary>
public sealed record SubmitExpenseForApprovalCommand(long Id) : IRequest<ApiResponse<ExpenseDto>>;

internal sealed class SubmitExpenseForApprovalCommandHandler
    : IRequestHandler<SubmitExpenseForApprovalCommand, ApiResponse<ExpenseDto>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    private readonly IApprovalService _approval;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public SubmitExpenseForApprovalCommandHandler(
        IRepository<Domain.Entities.Expense, long> repo,
        IApprovalService approval,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo; _approval = approval; _uow = uow; _mediator = mediator;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(SubmitExpenseForApprovalCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<ExpenseDto>.Fail("Expense not found.");
        if (e.Status != ExpenseStatus.Draft)
            return ApiResponse<ExpenseDto>.Fail("Only draft expenses can be submitted for approval.");

        // Expenses are BDT — Amount is the threshold comparison value.
        var result = await _approval.SubmitAsync("Expense", e.Id, e.Code, e.Amount, ct);

        if (result.AutoApproved)
        {
            // Within threshold → record + pay now. ApproveExpenseCommand commits the approval
            // request rows (tracked by the same context) together with the expense + journal.
            return await _mediator.Send(new ApproveExpenseCommand(e.Id), ct);
        }

        e.Status = ExpenseStatus.PendingApproval;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetExpenseByIdQuery(e.Id), ct);
    }
}

/// <summary>Cancels an expense. Approved → posts a mirror reversal; Draft → just cancels.</summary>
public sealed record CancelExpenseCommand(long Id) : IRequest<ApiResponse<ExpenseDto>>;

internal sealed class CancelExpenseCommandHandler : IRequestHandler<CancelExpenseCommand, ApiResponse<ExpenseDto>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    private readonly IRepository<ExpenseCategory> _categoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public CancelExpenseCommandHandler(
        IRepository<Domain.Entities.Expense, long> repo,
        IRepository<ExpenseCategory> categoryRepo,
        IUnitOfWork uow,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _uow = uow;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(CancelExpenseCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<ExpenseDto>.Fail("Expense not found.");
        if (e.Status == ExpenseStatus.Cancelled)
            return ApiResponse<ExpenseDto>.Fail("Expense is already cancelled.");

        var wasApproved = e.Status == ExpenseStatus.Approved;
        e.Status = ExpenseStatus.Cancelled;
        _repo.Update(e);

        if (wasApproved)
        {
            var category = await _categoryRepo.Query()
                .Include(c => c.LedgerAccount)
                .FirstOrDefaultAsync(c => c.Id == e.ExpenseCategoryId, ct);
            var expenseAccount = category?.LedgerAccount?.Code ?? LedgerAccounts.AdministrativeExpense;
            var cashAccount = e.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;

            await _journal.PostAsync(
                DateOnly.FromDateTime(DateTime.UtcNow), $"Reversal of expense {e.Code}", "ExpenseReversal", e.Id, e.Code,
                new[]
                {
                    new JournalPostingLine(cashAccount, e.Amount, 0m),
                    new JournalPostingLine(expenseAccount, 0m, e.Amount),
                }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetExpenseByIdQuery(e.Id), ct);
    }
}

/// <summary>Deletes a draft expense (soft delete). Approved expenses must be cancelled, not deleted.</summary>
public sealed record DeleteExpenseCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteExpenseCommandHandler(IRepository<Domain.Entities.Expense, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteExpenseCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Expense not found.");
        if (e.Status != ExpenseStatus.Draft)
            return ApiResponse.Fail("Only draft expenses can be deleted (cancel an approved expense instead).");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Expense deleted.");
    }
}
