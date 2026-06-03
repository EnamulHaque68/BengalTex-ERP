using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.CreditNotes.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CreditNotes.Commands;

// ───────────────────────────────────────────────────────────────────────────
//   List
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetCreditNotesQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    int? CustomerId = null,
    long? CustomerInvoiceId = null
) : IRequest<ApiResponse<PagedResult<CreditNoteDto>>>;

internal sealed class GetCreditNotesQueryHandler
    : IRequestHandler<GetCreditNotesQuery, ApiResponse<PagedResult<CreditNoteDto>>>
{
    private readonly IRepository<CreditNote, long> _repo;
    public GetCreditNotesQueryHandler(IRepository<CreditNote, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<CreditNoteDto>>> Handle(
        GetCreditNotesQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(req.Status)
            && Enum.TryParse<CreditNoteStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (req.CustomerId.HasValue) q = q.Where(x => x.CustomerId == req.CustomerId.Value);
        if (req.CustomerInvoiceId.HasValue) q = q.Where(x => x.CustomerInvoiceId == req.CustomerInvoiceId.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search)
                          || x.Customer.Name.Contains(search)
                          || x.CustomerInvoice.Code.Contains(search));

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(x => new CreditNoteDto(
                x.Id, x.Code, x.CustomerId, x.Customer.Name,
                x.CustomerInvoiceId, x.CustomerInvoice.Code,
                x.CustomerInvoice.TotalAmount, x.CustomerInvoice.AmountPaid,
                x.IssueDate, x.Reason.ToString(), x.Amount,
                x.CurrencyId, x.Currency.Code, x.ExchangeRate,
                x.Status.ToString(),
                x.IssuedAt, x.IssuedBy, x.Notes))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<CreditNoteDto>>.Ok(
            PagedResult<CreditNoteDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Get By Id
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetCreditNoteByIdQuery(long Id) : IRequest<ApiResponse<CreditNoteDto>>;

internal sealed class GetCreditNoteByIdQueryHandler
    : IRequestHandler<GetCreditNoteByIdQuery, ApiResponse<CreditNoteDto>>
{
    private readonly IRepository<CreditNote, long> _repo;
    public GetCreditNoteByIdQueryHandler(IRepository<CreditNote, long> repo) => _repo = repo;

    public async Task<ApiResponse<CreditNoteDto>> Handle(GetCreditNoteByIdQuery q, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(x => x.Id == q.Id)
            .Select(x => new CreditNoteDto(
                x.Id, x.Code, x.CustomerId, x.Customer.Name,
                x.CustomerInvoiceId, x.CustomerInvoice.Code,
                x.CustomerInvoice.TotalAmount, x.CustomerInvoice.AmountPaid,
                x.IssueDate, x.Reason.ToString(), x.Amount,
                x.CurrencyId, x.Currency.Code, x.ExchangeRate,
                x.Status.ToString(),
                x.IssuedAt, x.IssuedBy, x.Notes))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? ApiResponse<CreditNoteDto>.Fail("Credit note not found.")
            : ApiResponse<CreditNoteDto>.Ok(dto);
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Create Draft
// ───────────────────────────────────────────────────────────────────────────
public sealed record CreateCreditNoteCommand(
    long CustomerInvoiceId,
    DateOnly IssueDate,
    string Reason,
    decimal Amount,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateCreditNoteCommandValidator : AbstractValidator<CreateCreditNoteCommand>
{
    public CreateCreditNoteCommandValidator()
    {
        RuleFor(x => x.CustomerInvoiceId).GreaterThan(0);
        RuleFor(x => x.IssueDate).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => Enum.TryParse<CreditDebitNoteReason>(r, out _))
            .WithMessage("Reason must be one of: PriceCorrection, QualityAllowance, Discount, WriteOff, Other.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateCreditNoteCommandHandler
    : IRequestHandler<CreateCreditNoteCommand, ApiResponse<long>>
{
    private readonly IRepository<CreditNote, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateCreditNoteCommandHandler(
        IRepository<CreditNote, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _invRepo = invRepo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(CreateCreditNoteCommand cmd, CancellationToken ct)
    {
        var inv = await _invRepo.GetByIdAsync(cmd.CustomerInvoiceId, ct);
        if (inv is null) return ApiResponse<long>.Fail("Customer invoice not found.");

        // Allow against Issued / PartiallyPaid / Paid (e.g. retroactive discount). Block Draft + Cancelled.
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Draft
            || inv.Status == Domain.Entities.CustomerInvoiceStatus.Cancelled)
            return ApiResponse<long>.Fail("Credit notes can only be issued against an Issued or paid invoice.");

        var outstanding = inv.TotalAmount - inv.AmountPaid;
        if (cmd.Amount > outstanding && cmd.Amount > inv.TotalAmount)
            return ApiResponse<long>.Fail(
                $"Credit amount {cmd.Amount:0.####} exceeds invoice total {inv.TotalAmount:0.####}.");

        var code = await _numbering.NextAsync("CN", null, ct);
        var entity = new CreditNote
        {
            Code = code,
            CustomerId = inv.CustomerId,
            CustomerInvoiceId = inv.Id,
            IssueDate = cmd.IssueDate,
            Reason = Enum.Parse<CreditDebitNoteReason>(cmd.Reason),
            Amount = cmd.Amount,
            CurrencyId = inv.CurrencyId,
            ExchangeRate = inv.ExchangeRate,
            Status = CreditNoteStatus.Draft,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Credit note draft created.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Update (Draft only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record UpdateCreditNoteCommand(
    long Id,
    DateOnly IssueDate,
    string Reason,
    decimal Amount,
    string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateCreditNoteCommandValidator : AbstractValidator<UpdateCreditNoteCommand>
{
    public UpdateCreditNoteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => Enum.TryParse<CreditDebitNoteReason>(r, out _));
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateCreditNoteCommandHandler
    : IRequestHandler<UpdateCreditNoteCommand, ApiResponse>
{
    private readonly IRepository<CreditNote, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateCreditNoteCommandHandler(IRepository<CreditNote, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateCreditNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Credit note not found.");
        if (n.Status != CreditNoteStatus.Draft)
            return ApiResponse.Fail($"Cannot edit a {n.Status} credit note.");

        n.IssueDate = cmd.IssueDate;
        n.Reason = Enum.Parse<CreditDebitNoteReason>(cmd.Reason);
        n.Amount = cmd.Amount;
        n.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(n);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Credit note updated.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Delete (Draft only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record DeleteCreditNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteCreditNoteCommandHandler
    : IRequestHandler<DeleteCreditNoteCommand, ApiResponse>
{
    private readonly IRepository<CreditNote, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteCreditNoteCommandHandler(IRepository<CreditNote, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteCreditNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Credit note not found.");
        if (n.Status != CreditNoteStatus.Draft)
            return ApiResponse.Fail($"Cannot delete a {n.Status} credit note. Cancel it first.");
        _repo.Remove(n);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Credit note deleted.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Issue (Draft → Issued): adjusts invoice + auto-journal
// ───────────────────────────────────────────────────────────────────────────
public sealed record IssueCreditNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class IssueCreditNoteCommandHandler
    : IRequestHandler<IssueCreditNoteCommand, ApiResponse>
{
    private readonly IRepository<CreditNote, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;

    public IssueCreditNoteCommandHandler(
        IRepository<CreditNote, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        IJournalPostingService journal,
        ICurrentUserService currentUser)
    {
        _repo = repo; _invRepo = invRepo; _uow = uow; _journal = journal; _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(IssueCreditNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Credit note not found.");
        if (n.Status != CreditNoteStatus.Draft)
            return ApiResponse.Fail($"Cannot issue a {n.Status} credit note.");

        var inv = await _invRepo.GetByIdAsync(n.CustomerInvoiceId, ct);
        if (inv is null) return ApiResponse.Fail("Source customer invoice not found.");

        var outstanding = inv.TotalAmount - inv.AmountPaid;
        if (n.Amount > outstanding)
            return ApiResponse.Fail(
                $"Credit amount {n.Amount:0.####} exceeds outstanding balance {outstanding:0.####} on invoice {inv.Code}.");

        // Apply the credit to the invoice (treated as a non-cash settlement)
        inv.AmountPaid += n.Amount;
        inv.Status = inv.AmountPaid >= inv.TotalAmount
            ? Domain.Entities.CustomerInvoiceStatus.Paid
            : Domain.Entities.CustomerInvoiceStatus.PartiallyPaid;
        _invRepo.Update(inv);

        n.Status = CreditNoteStatus.Issued;
        n.IssuedAt = DateTimeOffset.UtcNow;
        n.IssuedBy = _currentUser.UserName ?? "system";
        _repo.Update(n);
        await _uow.SaveChangesAsync(ct);

        // Auto-journal: Dr Sales Returns / Cr AR — in base BDT
        var baseAmount = n.Amount * n.ExchangeRate;
        await _journal.PostAsync(
            n.IssueDate, $"Credit Note {n.Code} against {inv.Code}", "CreditNote", n.Id, n.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.SalesReturnsAllowances, baseAmount, 0m),
                new JournalPostingLine(LedgerAccounts.AccountsReceivable, 0m, baseAmount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Credit note {n.Code} issued.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Cancel (Issued → Cancelled): reverses invoice adjustment + posts mirror journal
// ───────────────────────────────────────────────────────────────────────────
public sealed record CancelCreditNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CancelCreditNoteCommandHandler
    : IRequestHandler<CancelCreditNoteCommand, ApiResponse>
{
    private readonly IRepository<CreditNote, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public CancelCreditNoteCommandHandler(
        IRepository<CreditNote, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        IJournalPostingService journal)
    {
        _repo = repo; _invRepo = invRepo; _uow = uow; _journal = journal;
    }

    public async Task<ApiResponse> Handle(CancelCreditNoteCommand cmd, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(cmd.Id, ct);
        if (n is null) return ApiResponse.Fail("Credit note not found.");
        if (n.Status == CreditNoteStatus.Cancelled)
            return ApiResponse.Fail("Credit note is already cancelled.");
        if (n.Status == CreditNoteStatus.Draft)
        {
            // Draft cancel = just delete (no side effects yet)
            _repo.Remove(n);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse.Ok("Draft credit note cancelled.");
        }

        // Issued → Cancelled: reverse invoice AmountPaid + recompute status
        var inv = await _invRepo.GetByIdAsync(n.CustomerInvoiceId, ct);
        if (inv is null) return ApiResponse.Fail("Source invoice not found.");
        inv.AmountPaid -= n.Amount;
        if (inv.AmountPaid < 0m) inv.AmountPaid = 0m;
        inv.Status = inv.AmountPaid <= 0m
            ? Domain.Entities.CustomerInvoiceStatus.Issued
            : Domain.Entities.CustomerInvoiceStatus.PartiallyPaid;
        _invRepo.Update(inv);

        n.Status = CreditNoteStatus.Cancelled;
        _repo.Update(n);
        await _uow.SaveChangesAsync(ct);

        // Mirror reversal: Dr AR / Cr Sales Returns
        var baseAmount = n.Amount * n.ExchangeRate;
        await _journal.PostAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            $"Cancel Credit Note {n.Code} (reversal)", "CreditNoteCancel", n.Id, n.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.AccountsReceivable, baseAmount, 0m),
                new JournalPostingLine(LedgerAccounts.SalesReturnsAllowances, 0m, baseAmount),
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Credit note {n.Code} cancelled.");
    }
}
