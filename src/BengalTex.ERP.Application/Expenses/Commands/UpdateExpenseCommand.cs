using BengalTex.ERP.Application.Expenses.Dtos;
using BengalTex.ERP.Application.Expenses.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Expenses.Commands;

public sealed record UpdateExpenseCommand(
    long Id,
    DateOnly ExpenseDate,
    int ExpenseCategoryId,
    decimal Amount,
    string PaymentMethod,
    string? Payee,
    string? ReferenceNumber,
    string? Description
) : IRequest<ApiResponse<ExpenseDto>>;

public sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.ExpenseCategoryId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Payee).MaximumLength(200);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

internal sealed class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand, ApiResponse<ExpenseDto>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    private readonly IRepository<ExpenseCategory> _categoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateExpenseCommandHandler(
        IRepository<Domain.Entities.Expense, long> repo,
        IRepository<ExpenseCategory> categoryRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(UpdateExpenseCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<ExpenseDto>.Fail("Expense not found.");
        if (e.Status != ExpenseStatus.Draft)
            return ApiResponse<ExpenseDto>.Fail("Only draft expenses can be edited.");

        var category = await _categoryRepo.GetByIdAsync(cmd.ExpenseCategoryId, ct);
        if (category is null) return ApiResponse<ExpenseDto>.Fail("Expense category not found.");

        e.ExpenseDate = cmd.ExpenseDate;
        e.ExpenseCategoryId = cmd.ExpenseCategoryId;
        e.Amount = cmd.Amount;
        e.PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        e.Payee = string.IsNullOrWhiteSpace(cmd.Payee) ? null : cmd.Payee.Trim();
        e.ReferenceNumber = string.IsNullOrWhiteSpace(cmd.ReferenceNumber) ? null : cmd.ReferenceNumber.Trim();
        e.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetExpenseByIdQuery(e.Id), ct);
    }
}
