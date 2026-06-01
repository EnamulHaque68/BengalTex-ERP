using BengalTex.ERP.Application.MasterSetup.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.MasterSetup.Commands;

// ── List ──
public sealed record GetBankAccountsQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<BankAccountDto>>>;

internal sealed class GetBankAccountsQueryHandler : IRequestHandler<GetBankAccountsQuery, ApiResponse<IReadOnlyList<BankAccountDto>>>
{
    private readonly IRepository<BankAccount> _repo;
    public GetBankAccountsQueryHandler(IRepository<BankAccount> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<BankAccountDto>>> Handle(GetBankAccountsQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(b => b.IsActive);
        var items = await q.OrderBy(b => b.BankName).ThenBy(b => b.AccountName)
            .Select(b => new BankAccountDto(
                b.Id, b.AccountName, b.BankName, b.BranchName,
                b.AccountNumber, b.AccountType.ToString(),
                b.RoutingNumber, b.SwiftCode, b.Currency,
                b.LedgerAccountId,
                b.LedgerAccount != null ? b.LedgerAccount.Code : null,
                b.LedgerAccount != null ? b.LedgerAccount.Name : null,
                b.Notes, b.IsActive))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<BankAccountDto>>.Ok(items);
    }
}

public sealed record CreateBankAccountCommand(
    string AccountName, string BankName, string? BranchName,
    string AccountNumber, string AccountType,
    string? RoutingNumber, string? SwiftCode, string Currency,
    int? LedgerAccountId, string? Notes
) : IRequest<ApiResponse<int>>;

public sealed record UpdateBankAccountCommand(
    int Id, string AccountName, string BankName, string? BranchName,
    string AccountNumber, string AccountType,
    string? RoutingNumber, string? SwiftCode, string Currency,
    int? LedgerAccountId, string? Notes, bool IsActive
) : IRequest<ApiResponse<int>>;

public sealed record DeleteBankAccountCommand(int Id) : IRequest<ApiResponse>;

public sealed class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountCommandValidator()
    {
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BranchName).MaximumLength(150);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountType).NotEmpty().Must(s => Enum.TryParse<BankAccountType>(s, out _));
        RuleFor(x => x.RoutingNumber).MaximumLength(30);
        RuleFor(x => x.SwiftCode).MaximumLength(20);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateBankAccountCommandValidator : AbstractValidator<UpdateBankAccountCommand>
{
    public UpdateBankAccountCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BranchName).MaximumLength(150);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountType).NotEmpty().Must(s => Enum.TryParse<BankAccountType>(s, out _));
        RuleFor(x => x.RoutingNumber).MaximumLength(30);
        RuleFor(x => x.SwiftCode).MaximumLength(20);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, ApiResponse<int>>
{
    private readonly IRepository<BankAccount> _repo;
    private readonly IUnitOfWork _uow;
    public CreateBankAccountCommandHandler(IRepository<BankAccount> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateBankAccountCommand cmd, CancellationToken ct)
    {
        var b = new BankAccount
        {
            AccountName = cmd.AccountName.Trim(),
            BankName = cmd.BankName.Trim(),
            BranchName = string.IsNullOrWhiteSpace(cmd.BranchName) ? null : cmd.BranchName.Trim(),
            AccountNumber = cmd.AccountNumber.Trim(),
            AccountType = Enum.Parse<BankAccountType>(cmd.AccountType),
            RoutingNumber = string.IsNullOrWhiteSpace(cmd.RoutingNumber) ? null : cmd.RoutingNumber.Trim(),
            SwiftCode = string.IsNullOrWhiteSpace(cmd.SwiftCode) ? null : cmd.SwiftCode.Trim().ToUpperInvariant(),
            Currency = cmd.Currency.Trim().ToUpperInvariant(),
            LedgerAccountId = cmd.LedgerAccountId,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(b, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(b.Id, "Bank account created.");
    }
}

internal sealed class UpdateBankAccountCommandHandler : IRequestHandler<UpdateBankAccountCommand, ApiResponse<int>>
{
    private readonly IRepository<BankAccount> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateBankAccountCommandHandler(IRepository<BankAccount> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateBankAccountCommand cmd, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) return ApiResponse<int>.Fail("Bank account not found.");
        b.AccountName = cmd.AccountName.Trim();
        b.BankName = cmd.BankName.Trim();
        b.BranchName = string.IsNullOrWhiteSpace(cmd.BranchName) ? null : cmd.BranchName.Trim();
        b.AccountNumber = cmd.AccountNumber.Trim();
        b.AccountType = Enum.Parse<BankAccountType>(cmd.AccountType);
        b.RoutingNumber = string.IsNullOrWhiteSpace(cmd.RoutingNumber) ? null : cmd.RoutingNumber.Trim();
        b.SwiftCode = string.IsNullOrWhiteSpace(cmd.SwiftCode) ? null : cmd.SwiftCode.Trim().ToUpperInvariant();
        b.Currency = cmd.Currency.Trim().ToUpperInvariant();
        b.LedgerAccountId = cmd.LedgerAccountId;
        b.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        b.IsActive = cmd.IsActive;
        _repo.Update(b);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(b.Id, "Bank account updated.");
    }
}

internal sealed class DeleteBankAccountCommandHandler : IRequestHandler<DeleteBankAccountCommand, ApiResponse>
{
    private readonly IRepository<BankAccount> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    public DeleteBankAccountCommandHandler(IRepository<BankAccount> repo, IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    { _repo = repo; _empRepo = empRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteBankAccountCommand cmd, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) return ApiResponse.Fail("Bank account not found.");
        if (await _empRepo.Query().AnyAsync(e => e.BankAccountId == cmd.Id, ct))
            return ApiResponse.Fail("This bank account is referenced by employees (deactivate instead).");
        _repo.Remove(b);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Bank account deleted.");
    }
}
