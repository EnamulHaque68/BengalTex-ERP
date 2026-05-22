using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

/// <summary>Freezes a draft voucher into the ledger. Re-checks the debit = credit balance.</summary>
public sealed record PostJournalEntryCommand(long Id) : IRequest<ApiResponse<JournalEntryDto>>;

internal sealed class PostJournalEntryCommandHandler
    : IRequestHandler<PostJournalEntryCommand, ApiResponse<JournalEntryDto>>
{
    private readonly IRepository<JournalEntry, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostJournalEntryCommandHandler(
        IRepository<JournalEntry, long> repo, IUnitOfWork uow,
        ICurrentUserService currentUser, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<JournalEntryDto>> Handle(
        PostJournalEntryCommand cmd, CancellationToken cancellationToken)
    {
        var entry = await _repo.Query()
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == cmd.Id, cancellationToken);
        if (entry is null) return ApiResponse<JournalEntryDto>.Fail("Journal voucher not found.");
        if (entry.Status != JournalEntryStatus.Draft)
            return ApiResponse<JournalEntryDto>.Fail("Only draft journal vouchers can be posted.");
        if (entry.Lines.Count < 2)
            return ApiResponse<JournalEntryDto>.Fail("A journal voucher needs at least two lines.");

        var debit = entry.Lines.Sum(l => l.Debit);
        var credit = entry.Lines.Sum(l => l.Credit);
        if (debit <= 0 || debit != credit)
            return ApiResponse<JournalEntryDto>.Fail($"Voucher is not balanced (Dr {debit:N2} vs Cr {credit:N2}).");

        entry.Status = JournalEntryStatus.Posted;
        entry.PostedAt = DateTimeOffset.UtcNow;
        entry.PostedBy = _currentUser.UserName;

        _repo.Update(entry);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetJournalEntryByIdQuery(entry.Id), cancellationToken);
    }
}

/// <summary>Deletes a draft voucher (soft delete). Posted vouchers are immutable.</summary>
public sealed record DeleteJournalEntryCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteJournalEntryCommandHandler
    : IRequestHandler<DeleteJournalEntryCommand, ApiResponse>
{
    private readonly IRepository<JournalEntry, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteJournalEntryCommandHandler(IRepository<JournalEntry, long> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteJournalEntryCommand cmd, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entry is null) return ApiResponse.Fail("Journal voucher not found.");
        if (entry.Status != JournalEntryStatus.Draft)
            return ApiResponse.Fail("Only draft journal vouchers can be deleted.");

        _repo.Remove(entry);
        await _uow.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Journal voucher deleted.");
    }
}
