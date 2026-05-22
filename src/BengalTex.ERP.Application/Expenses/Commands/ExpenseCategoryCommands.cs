using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Expenses.Commands;

// ─── Create ──────────────────────────────────────────────────────────────────
public sealed record CreateExpenseCategoryCommand(
    string Name, int? LedgerAccountId, string? Description) : IRequest<ApiResponse<int>>;

public sealed class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateExpenseCategoryCommandHandler : IRequestHandler<CreateExpenseCategoryCommand, ApiResponse<int>>
{
    private readonly IRepository<ExpenseCategory> _repo;
    private readonly IUnitOfWork _uow;
    public CreateExpenseCategoryCommandHandler(IRepository<ExpenseCategory> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateExpenseCategoryCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        if (await _repo.Query().AnyAsync(c => c.Name == name, ct))
            return ApiResponse<int>.Fail($"An expense category '{name}' already exists.");

        var entity = new ExpenseCategory
        {
            Name = name,
            LedgerAccountId = cmd.LedgerAccountId,
            IsActive = true,
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(entity.Id, "Expense category created.");
    }
}

// ─── Update ──────────────────────────────────────────────────────────────────
public sealed record UpdateExpenseCategoryCommand(
    int Id, string Name, int? LedgerAccountId, bool IsActive, string? Description) : IRequest<ApiResponse<int>>;

public sealed class UpdateExpenseCategoryCommandValidator : AbstractValidator<UpdateExpenseCategoryCommand>
{
    public UpdateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateExpenseCategoryCommandHandler : IRequestHandler<UpdateExpenseCategoryCommand, ApiResponse<int>>
{
    private readonly IRepository<ExpenseCategory> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateExpenseCategoryCommandHandler(IRepository<ExpenseCategory> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateExpenseCategoryCommand cmd, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(cmd.Id, ct);
        if (c is null) return ApiResponse<int>.Fail("Expense category not found.");
        var name = cmd.Name.Trim();
        if (name != c.Name && await _repo.Query().AnyAsync(x => x.Name == name && x.Id != cmd.Id, ct))
            return ApiResponse<int>.Fail($"An expense category '{name}' already exists.");

        c.Name = name;
        c.LedgerAccountId = cmd.LedgerAccountId;
        c.IsActive = cmd.IsActive;
        c.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        _repo.Update(c);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(c.Id, "Expense category updated.");
    }
}

// ─── Delete ──────────────────────────────────────────────────────────────────
public sealed record DeleteExpenseCategoryCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteExpenseCategoryCommandHandler : IRequestHandler<DeleteExpenseCategoryCommand, ApiResponse>
{
    private readonly IRepository<ExpenseCategory> _repo;
    private readonly IRepository<Domain.Entities.Expense, long> _expenseRepo;
    private readonly IUnitOfWork _uow;
    public DeleteExpenseCategoryCommandHandler(IRepository<ExpenseCategory> repo, IRepository<Domain.Entities.Expense, long> expenseRepo, IUnitOfWork uow)
    { _repo = repo; _expenseRepo = expenseRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteExpenseCategoryCommand cmd, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(cmd.Id, ct);
        if (c is null) return ApiResponse.Fail("Expense category not found.");
        if (await _expenseRepo.Query().AnyAsync(e => e.ExpenseCategoryId == cmd.Id, ct))
            return ApiResponse.Fail("This category is used by expenses (deactivate it instead).");
        _repo.Remove(c);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Expense category deleted.");
    }
}
