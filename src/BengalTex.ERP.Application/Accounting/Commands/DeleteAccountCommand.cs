using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

public sealed record DeleteAccountCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteAccountCommandHandler
    : IRequestHandler<DeleteAccountCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Account> _repo;
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IUnitOfWork _uow;

    public DeleteAccountCommandHandler(
        IRepository<Domain.Entities.Account> repo,
        IRepository<JournalEntryLine, long> lineRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _lineRepo = lineRepo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteAccountCommand cmd, CancellationToken cancellationToken)
    {
        var account = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (account is null) return ApiResponse.Fail("Account not found.");
        if (account.IsSystem) return ApiResponse.Fail("System accounts cannot be deleted.");

        if (await _repo.Query().AnyAsync(a => a.ParentAccountId == cmd.Id, cancellationToken))
            return ApiResponse.Fail("This account has child accounts — remove or re-parent them first.");

        if (await _lineRepo.Query().AnyAsync(l => l.AccountId == cmd.Id, cancellationToken))
            return ApiResponse.Fail("This account has journal postings and cannot be deleted (deactivate it instead).");

        _repo.Remove(account);   // soft delete
        await _uow.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Account deleted.");
    }
}
