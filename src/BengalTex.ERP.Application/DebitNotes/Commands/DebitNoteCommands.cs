using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.DebitNotes.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.DebitNotes.Commands;

// ───────────────────────────────────────────────────────────────────────────
//   List
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetDebitNotesQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    int? SupplierId = null,
    long? SupplierInvoiceId = null
) : IRequest<ApiResponse<PagedResult<DebitNoteDto>>>;

internal sealed class GetDebitNotesQueryHandler
    : IRequestHandler<GetDebitNotesQuery, ApiResponse<PagedResult<DebitNoteDto>>>
{
    private readonly IRepository<DebitNote, long> _repo;
    public GetDebitNotesQueryHandler(IRepository<DebitNote, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<DebitNoteDto>>> Handle(
        GetDebitNotesQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(req.Status)
            && Enum.TryParse<DebitNoteStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (req.SupplierId.HasValue) q = q.Where(x => x.SupplierId == req.SupplierId.Value);
        if (req.SupplierInvoiceId.HasValue) q = q.Where(x => x.SupplierInvoiceId == req.SupplierInvoiceId.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search)
                          || x.Supplier.Name.Contains(search)
                          || x.SupplierInvoice.Code.Contains(search));

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(x => new DebitNoteDto(
                x.Id, x.Code, x.SupplierId, x.Supplier.Name,
                x.SupplierInvoiceId, x.SupplierInvoice.Code,
                x.SupplierInvoice.TotalAmount, x.SupplierInvoice.AmountPaid,
                x.IssueDate, x.Reason.ToString(), x.Amount,
                x.CurrencyId, x.Currency.Code, x.ExchangeRate,
                x.Status.ToString(),
                x.IssuedAt, x.IssuedBy, x.Notes))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<DebitNoteDto>>.Ok(
            PagedResult<DebitNoteDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Get By Id
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetDebitNoteByIdQuery(long Id) : IRequest<ApiResponse<DebitNoteDto>>;

internal sealed class GetDebitNoteByIdQueryHandler
    : IRequestHandler<GetDebitNoteByIdQuery, ApiResponse<DebitNoteDto>>
{
    private readonly IRepository<DebitNote, long> _repo;
    public GetDebitNoteByIdQueryHandler(IRepository<DebitNote, long> repo) => _repo = repo;

    public async Task<ApiResponse<DebitNoteDto>> Handle(GetDebitNoteByIdQuery q, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(x => x.Id == q.Id)
            .Select(x => new DebitNoteDto(
                x.Id, x.Code, x.SupplierId, x.Supplier.Name,
                x.SupplierInvoiceId, x.SupplierInvoice.Code,
                x.SupplierInvoice.TotalAmount, x.SupplierInvoice.AmountPaid,
                x.IssueDate, x.Reason.ToString(), x.Amount,
                x.CurrencyId, x.Currency.Code, x.ExchangeRate,
                x.Status.ToString(),
                x.IssuedAt, x.IssuedBy, x.Notes))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? ApiResponse<DebitNoteDto>.Fail("Debit note not found.")
            : ApiResponse<DebitNoteDto>.Ok(dto);
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Create Draft
// ───────────────────────────────────────────────────────────────────────────
public sealed record CreateDebitNoteCommand(
    long SupplierInvoiceId,
    DateOnly IssueDate,
    string Reason,
    decimal Amount,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateDebitNoteCommandValidator : AbstractValidator<CreateDebitNoteCommand>
{
    public CreateDebitNoteCommandValidator()
    {
        RuleFor(x => x.SupplierInvoiceId).GreaterThan(0);
        RuleFor(x => x.IssueDate).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => Enum.TryParse<CreditDebitNoteReason>(r, out _))
            .WithMessage("Reason must be one of: PriceCorrection, QualityAllowance, Discount, WriteOff, Other.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateDebitNoteCommandHandler
    : IRequestHandler<CreateDebitNoteCommand, ApiResponse<long>>
{
    private readonly IRepository<DebitNote, long> _repo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateDebitNoteCommandHandler(
        IRepository<DebitNote, long> repo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _invRepo = invRepo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(CreateDebitNoteCommand cmd, CancellationToken ct)
    {
        var inv = await _invRepo.GetByIdAsync(cmd.SupplierInvoiceId, ct);
        if (inv is null) return ApiResponse<long>.Fail("Supplier invoice not found.");

        if (inv.Status == Domain.Entities.SupplierInvoiceStatus.Draft
            || inv.Status == Domain.Entities.SupplierInvoiceStatus.Cancelled)
            return ApiResponse<long>.Fail("Debit notes can only be issued against an Approved or paid supplier invoice.");

        if (cmd.Amount > inv.TotalAmount)
            return ApiResponse<long>.Fail(
                $"Debit amount {cmd.Amount:0.####} exceeds invoice total {inv.TotalAmount:0.####}.");

        var code = await _numbering.NextAsync("DBN", null, ct);
        var entity = new DebitNote
        {
            Code = code,
            SupplierId = inv.SupplierId,
            SupplierInvoiceId = inv.Id,
            IssueDate = cmd.IssueDate,
            Reason = Enum.Parse<CreditDebitNoteReason>(cmd.Reason),
            Amount = cmd.Amount,
            CurrencyId = inv.CurrencyId,
            ExchangeRate = inv.ExchangeRate,
            Status = DebitNoteStatus.Draft,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Debit note draft created.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Update (Draft only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record UpdateDebitNoteCommand(
    long Id,
    DateOnly IssueDate,
    string Reason,
    decimal Amount,
    string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateDebitNoteCommandValidator : AbstractValidator<UpdateDebitNoteCommand>
{
    public UpdateDebitNoteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => Enum.TryParse<CreditDebitNoteReason>(r, out _));
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateDebitNoteCommandHandler
    : IRequestHandler<UpdateDebitNoteCommand, ApiResponse>
{
    private readonly IRepository<DebitNote, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateDebitNoteCommandHandler(IRepository<DebitNote, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateDebitNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Debit note not found.");
        if (n.Status != DebitNoteStatus.Draft)
            return ApiResponse.Fail($"Cannot edit a {n.Status} debit note.");

        n.IssueDate = cmd.IssueDate;
        n.Reason = Enum.Parse<CreditDebitNoteReason>(cmd.Reason);
        n.Amount = cmd.Amount;
        n.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(n);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Debit note updated.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Delete (Draft only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record DeleteDebitNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteDebitNoteCommandHandler
    : IRequestHandler<DeleteDebitNoteCommand, ApiResponse>
{
    private readonly IRepository<DebitNote, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteDebitNoteCommandHandler(IRepository<DebitNote, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteDebitNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Debit note not found.");
        if (n.Status != DebitNoteStatus.Draft)
            return ApiResponse.Fail($"Cannot delete a {n.Status} debit note. Cancel it first.");
        _repo.Remove(n);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Debit note deleted.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Issue (Draft → Issued): adjusts supplier invoice + auto-journal
// ───────────────────────────────────────────────────────────────────────────
public sealed record IssueDebitNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class IssueDebitNoteCommandHandler
    : IRequestHandler<IssueDebitNoteCommand, ApiResponse>
{
    private readonly IRepository<DebitNote, long> _repo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;

    public IssueDebitNoteCommandHandler(
        IRepository<DebitNote, long> repo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IUnitOfWork uow,
        IJournalPostingService journal,
        ICurrentUserService currentUser)
    {
        _repo = repo; _invRepo = invRepo; _uow = uow; _journal = journal; _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(IssueDebitNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Debit note not found.");
        if (n.Status != DebitNoteStatus.Draft)
            return ApiResponse.Fail($"Cannot issue a {n.Status} debit note.");

        var inv = await _invRepo.GetByIdAsync(n.SupplierInvoiceId, ct);
        if (inv is null) return ApiResponse.Fail("Source supplier invoice not found.");

        var outstanding = inv.TotalAmount - inv.AmountPaid;
        if (n.Amount > outstanding)
            return ApiResponse.Fail(
                $"Debit amount {n.Amount:0.####} exceeds outstanding balance {outstanding:0.####} on invoice {inv.Code}.");

        inv.AmountPaid += n.Amount;
        inv.Status = inv.AmountPaid >= inv.TotalAmount
            ? Domain.Entities.SupplierInvoiceStatus.Paid
            : Domain.Entities.SupplierInvoiceStatus.PartiallyPaid;
        _invRepo.Update(inv);

        n.Status = DebitNoteStatus.Issued;
        n.IssuedAt = DateTimeOffset.UtcNow;
        n.IssuedBy = _currentUser.UserName ?? "system";
        _repo.Update(n);
        await _uow.SaveChangesAsync(ct);

        // Auto-journal: Dr AP / Cr Purchase Returns — in base BDT
        var baseAmount = n.Amount * n.ExchangeRate;
        await _journal.PostAsync(
            n.IssueDate, $"Debit Note {n.Code} against {inv.Code}", "DebitNote", n.Id, n.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.AccountsPayable, baseAmount, 0m),
                new JournalPostingLine(LedgerAccounts.PurchaseReturnsAllowances, 0m, baseAmount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Debit note {n.Code} issued.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Cancel (Issued → Cancelled)
// ───────────────────────────────────────────────────────────────────────────
public sealed record CancelDebitNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CancelDebitNoteCommandHandler
    : IRequestHandler<CancelDebitNoteCommand, ApiResponse>
{
    private readonly IRepository<DebitNote, long> _repo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public CancelDebitNoteCommandHandler(
        IRepository<DebitNote, long> repo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IUnitOfWork uow,
        IJournalPostingService journal)
    {
        _repo = repo; _invRepo = invRepo; _uow = uow; _journal = journal;
    }

    public async Task<ApiResponse> Handle(CancelDebitNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Debit note not found.");
        if (n.Status == DebitNoteStatus.Cancelled)
            return ApiResponse.Fail("Debit note is already cancelled.");
        if (n.Status == DebitNoteStatus.Draft)
        {
            _repo.Remove(n);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse.Ok("Draft debit note cancelled.");
        }

        var inv = await _invRepo.GetByIdAsync(n.SupplierInvoiceId, ct);
        if (inv is null) return ApiResponse.Fail("Source supplier invoice not found.");
        inv.AmountPaid -= n.Amount;
        if (inv.AmountPaid < 0m) inv.AmountPaid = 0m;
        inv.Status = inv.AmountPaid <= 0m
            ? Domain.Entities.SupplierInvoiceStatus.Approved
            : Domain.Entities.SupplierInvoiceStatus.PartiallyPaid;
        _invRepo.Update(inv);

        n.Status = DebitNoteStatus.Cancelled;
        _repo.Update(n);
        await _uow.SaveChangesAsync(ct);

        var baseAmount = n.Amount * n.ExchangeRate;
        await _journal.PostAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            $"Cancel Debit Note {n.Code} (reversal)", "DebitNoteCancel", n.Id, n.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.PurchaseReturnsAllowances, baseAmount, 0m),
                new JournalPostingLine(LedgerAccounts.AccountsPayable, 0m, baseAmount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Debit note {n.Code} cancelled.");
    }
}
