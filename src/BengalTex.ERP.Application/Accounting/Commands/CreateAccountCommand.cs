using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

public sealed record CreateAccountCommand(
    string Code,
    string Name,
    string AccountType,
    bool IsGroup,
    int? ParentAccountId,
    string? Description,
    bool RequiresCostCenter = false   // Phase A3
) : IRequest<ApiResponse<AccountDto>>;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountType).NotEmpty()
            .Must(t => Enum.TryParse<AccountType>(t, out _))
            .WithMessage("AccountType must be Asset, Liability, Equity, Income or Expense.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateAccountCommandHandler
    : IRequestHandler<CreateAccountCommand, ApiResponse<AccountDto>>
{
    private readonly IRepository<Domain.Entities.Account> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CreateAccountCommandHandler(
        IRepository<Domain.Entities.Account> repo, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<AccountDto>> Handle(
        CreateAccountCommand cmd, CancellationToken cancellationToken)
    {
        var code = cmd.Code.Trim();
        if (await _repo.Query().AnyAsync(a => a.Code == code, cancellationToken))
            return ApiResponse<AccountDto>.Fail($"An account with code '{code}' already exists.");

        var type = Enum.Parse<AccountType>(cmd.AccountType);

        if (cmd.ParentAccountId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(cmd.ParentAccountId.Value, cancellationToken);
            if (parent is null) return ApiResponse<AccountDto>.Fail("Parent account not found.");
            if (!parent.IsGroup)
                return ApiResponse<AccountDto>.Fail("Parent must be a group (header) account.");
            if (parent.AccountType != type)
                return ApiResponse<AccountDto>.Fail("Account type must match the parent's type.");
        }

        var entity = new Domain.Entities.Account
        {
            Code = code,
            Name = cmd.Name.Trim(),
            AccountType = type,
            IsGroup = cmd.IsGroup,
            ParentAccountId = cmd.ParentAccountId,
            IsSystem = false,
            IsActive = true,
            RequiresCostCenter = !cmd.IsGroup && cmd.RequiresCostCenter,   // Phase A3 — only detail accounts
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetAccountByIdQuery(entity.Id), cancellationToken);
    }
}
