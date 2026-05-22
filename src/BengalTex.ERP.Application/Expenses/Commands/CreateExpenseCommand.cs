using BengalTex.ERP.Application.Expenses.Dtos;
using BengalTex.ERP.Application.Expenses.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Expenses.Commands;

public sealed record CreateExpenseCommand(
    DateOnly ExpenseDate,
    int ExpenseCategoryId,
    decimal Amount,
    string PaymentMethod,
    string? Payee,
    string? ReferenceNumber,
    string? Description
) : IRequest<ApiResponse<ExpenseDto>>;

public sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
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

internal sealed class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, ApiResponse<ExpenseDto>>
{
    private readonly IRepository<Domain.Entities.Expense, long> _repo;
    private readonly IRepository<ExpenseCategory> _categoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateExpenseCommandHandler(
        IRepository<Domain.Entities.Expense, long> repo,
        IRepository<ExpenseCategory> categoryRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ExpenseDto>> Handle(CreateExpenseCommand cmd, CancellationToken ct)
    {
        var category = await _categoryRepo.GetByIdAsync(cmd.ExpenseCategoryId, ct);
        if (category is null) return ApiResponse<ExpenseDto>.Fail("Expense category not found.");

        var entity = new Domain.Entities.Expense
        {
            Code = await _numbering.NextAsync("EXP", null, ct),
            ExpenseDate = cmd.ExpenseDate,
            ExpenseCategoryId = cmd.ExpenseCategoryId,
            Amount = cmd.Amount,
            PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod),
            Payee = string.IsNullOrWhiteSpace(cmd.Payee) ? null : cmd.Payee.Trim(),
            ReferenceNumber = string.IsNullOrWhiteSpace(cmd.ReferenceNumber) ? null : cmd.ReferenceNumber.Trim(),
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            Status = ExpenseStatus.Draft
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetExpenseByIdQuery(entity.Id), ct);
    }
}
