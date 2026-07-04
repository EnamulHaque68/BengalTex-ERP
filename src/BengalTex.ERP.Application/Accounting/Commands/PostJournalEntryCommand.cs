using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Commands;

/// <summary>
/// Freezes a draft voucher into the ledger. Re-checks the debit = credit balance, enforces the
/// fiscal-period guard, and (Phase A1) routes over-threshold manual vouchers through the
/// Approvals engine — the voucher waits in <see cref="JournalEntryStatus.PendingApproval"/>
/// until decided. <paramref name="BypassApprovalGate"/> is set only by the approval decision
/// handler, once sign-off is granted.
/// </summary>
public sealed record PostJournalEntryCommand(long Id, bool BypassApprovalGate = false)
    : IRequest<ApiResponse<JournalEntryDto>>;

internal sealed class PostJournalEntryCommandHandler
    : IRequestHandler<PostJournalEntryCommand, ApiResponse<JournalEntryDto>>
{
    private readonly IRepository<JournalEntry, long> _repo;
    private readonly IPeriodGuard _periodGuard;
    private readonly IApprovalService _approval;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public PostJournalEntryCommandHandler(
        IRepository<JournalEntry, long> repo,
        IPeriodGuard periodGuard,
        IApprovalService approval,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _periodGuard = periodGuard;
        _approval = approval;
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

        var postableFromApproval = cmd.BypassApprovalGate && entry.Status == JournalEntryStatus.PendingApproval;
        if (entry.Status != JournalEntryStatus.Draft && !postableFromApproval)
            return ApiResponse<JournalEntryDto>.Fail(
                entry.Status == JournalEntryStatus.PendingApproval
                    ? "This voucher is awaiting approval — it will post when approved."
                    : "Only draft journal vouchers can be posted.");
        if (entry.Lines.Count < 2)
            return ApiResponse<JournalEntryDto>.Fail("A journal voucher needs at least two lines.");

        var debit = entry.Lines.Sum(l => l.Debit);
        var credit = entry.Lines.Sum(l => l.Credit);
        if (debit <= 0 || debit != credit)
            return ApiResponse<JournalEntryDto>.Fail($"Voucher is not balanced (Dr {debit:N2} vs Cr {credit:N2}).");

        // Phase A1 — fiscal-period guard (re-checked even on the approval path: the period may
        // have closed while the voucher waited in the inbox).
        var refusal = await _periodGuard.CheckAsync(entry.EntryDate, isManualVoucher: true, cancellationToken);
        if (refusal is not null) return ApiResponse<JournalEntryDto>.Fail(refusal);

        // Phase A1 — approval gate for over-threshold MANUAL vouchers (auto-journals never come
        // through this command; reversals/contra/opening post via their own commands).
        if (!cmd.BypassApprovalGate && entry.SourceType is null)
        {
            var submit = await _approval.SubmitAsync("JournalEntry", entry.Id, entry.Code, debit, cancellationToken);
            if (!submit.AutoApproved)
            {
                entry.Status = JournalEntryStatus.PendingApproval;
                _repo.Update(entry);
                await _uow.SaveChangesAsync(cancellationToken);   // approval request + status, atomic
                return await _mediator.Send(new GetJournalEntryByIdQuery(entry.Id), cancellationToken);
            }
        }

        entry.Status = JournalEntryStatus.Posted;
        entry.AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(entry.EntryDate, cancellationToken);
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
