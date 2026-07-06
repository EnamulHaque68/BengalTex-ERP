using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

public sealed record UpdateAccountCommand(
    int Id,
    string Code,
    string Name,
    string AccountType,
    bool IsGroup,
    int? ParentAccountId,
    bool IsActive,
    string? Description,
    bool RequiresCostCenter = false   // Phase A3
) : IRequest<ApiResponse<AccountDto>>;

public sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountType).NotEmpty()
            .Must(t => Enum.TryParse<AccountType>(t, out _))
            .WithMessage("AccountType must be Asset, Liability, Equity, Income or Expense.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateAccountCommandHandler
    : IRequestHandler<UpdateAccountCommand, ApiResponse<AccountDto>>
{
    private readonly IRepository<Domain.Entities.Account> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateAccountCommandHandler(
        IRepository<Domain.Entities.Account> repo, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<AccountDto>> Handle(
        UpdateAccountCommand cmd, CancellationToken cancellationToken)
    {
        var account = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (account is null) return ApiResponse<AccountDto>.Fail("Account not found.");

        var type = Enum.Parse<AccountType>(cmd.AccountType);
        var code = cmd.Code.Trim();

        // System accounts: protect Code / Type / Group structure; allow rename / re-parent / (de)activate.
        if (account.IsSystem && (code != account.Code || type != account.AccountType || cmd.IsGroup != account.IsGroup))
            return ApiResponse<AccountDto>.Fail("A system account's code, type and grouping cannot be changed.");

        if (code != account.Code &&
            await _repo.Query().AnyAsync(a => a.Code == code && a.Id != cmd.Id, cancellationToken))
            return ApiResponse<AccountDto>.Fail($"An account with code '{code}' already exists.");

        if (cmd.ParentAccountId.HasValue)
        {
            if (cmd.ParentAccountId.Value == cmd.Id)
                return ApiResponse<AccountDto>.Fail("An account cannot be its own parent.");
            var parent = await _repo.GetByIdAsync(cmd.ParentAccountId.Value, cancellationToken);
            if (parent is null) return ApiResponse<AccountDto>.Fail("Parent account not found.");
            if (!parent.IsGroup)
                return ApiResponse<AccountDto>.Fail("Parent must be a group (header) account.");
            if (parent.AccountType != type)
                return ApiResponse<AccountDto>.Fail("Account type must match the parent's type.");
        }

        account.Code = code;
        account.Name = cmd.Name.Trim();
        account.AccountType = type;
        account.IsGroup = cmd.IsGroup;
        account.ParentAccountId = cmd.ParentAccountId;
        account.IsActive = cmd.IsActive;
        account.RequiresCostCenter = !cmd.IsGroup && cmd.RequiresCostCenter;   // Phase A3 — only detail accounts
        account.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();

        _repo.Update(account);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetAccountByIdQuery(account.Id), cancellationToken);
    }
}
