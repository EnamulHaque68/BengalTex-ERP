using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.CustomerInvoice.Commands;
using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.ProformaInvoices.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProformaInvoices.Commands;

public sealed record ProformaInvoiceLineInput(
    int ProductId, decimal Quantity, decimal UnitPrice, string? LineNotes);

// ───────────────────────────────────────────────────────────────────────────
//   List
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetProformaInvoicesQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    int? CustomerId = null
) : IRequest<ApiResponse<PagedResult<ProformaInvoiceDto>>>;

internal sealed class GetProformaInvoicesQueryHandler
    : IRequestHandler<GetProformaInvoicesQuery, ApiResponse<PagedResult<ProformaInvoiceDto>>>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    public GetProformaInvoicesQueryHandler(IRepository<Domain.Entities.ProformaInvoice, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<ProformaInvoiceDto>>> Handle(
        GetProformaInvoicesQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(req.Status)
            && Enum.TryParse<ProformaInvoiceStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (req.CustomerId.HasValue) q = q.Where(x => x.CustomerId == req.CustomerId.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search) || x.Customer.Name.Contains(search));

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(x => new ProformaInvoiceDto(
                x.Id, x.Code, x.CustomerId, x.Customer.Name,
                x.SalesOrderId, x.SalesOrder != null ? x.SalesOrder.Code : null,
                x.IssueDate, x.ValidUntil, x.Status.ToString(),
                x.CurrencyId, x.Currency.Code, x.ExchangeRate,
                x.VatRate, x.SubtotalAmount, x.VatAmount, x.TotalAmount,
                x.SentAt, x.SentBy, x.AcceptedAt, x.ExpiredAt,
                x.ConvertedCustomerInvoiceId,
                x.ConvertedCustomerInvoice != null ? x.ConvertedCustomerInvoice.Code : null,
                x.Notes,
                new List<ProformaInvoiceLineDto>()))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<ProformaInvoiceDto>>.Ok(
            PagedResult<ProformaInvoiceDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Get By Id (with lines)
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetProformaInvoiceByIdQuery(long Id) : IRequest<ApiResponse<ProformaInvoiceDto>>;

internal sealed class GetProformaInvoiceByIdQueryHandler
    : IRequestHandler<GetProformaInvoiceByIdQuery, ApiResponse<ProformaInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    public GetProformaInvoiceByIdQueryHandler(IRepository<Domain.Entities.ProformaInvoice, long> repo) => _repo = repo;

    public async Task<ApiResponse<ProformaInvoiceDto>> Handle(GetProformaInvoiceByIdQuery q, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(x => x.Id == q.Id)
            .Select(x => new ProformaInvoiceDto(
                x.Id, x.Code, x.CustomerId, x.Customer.Name,
                x.SalesOrderId, x.SalesOrder != null ? x.SalesOrder.Code : null,
                x.IssueDate, x.ValidUntil, x.Status.ToString(),
                x.CurrencyId, x.Currency.Code, x.ExchangeRate,
                x.VatRate, x.SubtotalAmount, x.VatAmount, x.TotalAmount,
                x.SentAt, x.SentBy, x.AcceptedAt, x.ExpiredAt,
                x.ConvertedCustomerInvoiceId,
                x.ConvertedCustomerInvoice != null ? x.ConvertedCustomerInvoice.Code : null,
                x.Notes,
                x.Lines.OrderBy(l => l.SortOrder).Select(l => new ProformaInvoiceLineDto(
                    l.Id, l.ProductId, l.Product.Code, l.Product.Name,
                    l.Product.UnitOfMeasure != null ? l.Product.UnitOfMeasure.Code : null,
                    l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                    l.SortOrder, l.LineNotes)).ToList()))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? ApiResponse<ProformaInvoiceDto>.Fail("Proforma invoice not found.")
            : ApiResponse<ProformaInvoiceDto>.Ok(dto);
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Create Draft
// ───────────────────────────────────────────────────────────────────────────
public sealed record CreateProformaInvoiceCommand(
    int CustomerId,
    long? SalesOrderId,
    DateOnly IssueDate,
    DateOnly ValidUntil,
    int CurrencyId,
    decimal ExchangeRate,
    decimal VatRate,
    string? Notes,
    IReadOnlyList<ProformaInvoiceLineInput> Lines
) : IRequest<ApiResponse<long>>;

public sealed class CreateProformaInvoiceCommandValidator : AbstractValidator<CreateProformaInvoiceCommand>
{
    public CreateProformaInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.IssueDate).NotEmpty();
        RuleFor(x => x.ValidUntil).GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Valid Until must be on or after the issue date.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A proforma invoice must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("The same product appears more than once.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateProformaInvoiceCommandHandler
    : IRequestHandler<CreateProformaInvoiceCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateProformaInvoiceCommandHandler(
        IRepository<Domain.Entities.ProformaInvoice, long> repo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _customerRepo = customerRepo; _productRepo = productRepo;
        _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(CreateProformaInvoiceCommand cmd, CancellationToken ct)
    {
        if (!await _customerRepo.AnyAsync(c => c.Id == cmd.CustomerId, ct))
            return ApiResponse<long>.Fail("Customer not found.");

        var productIds = cmd.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existing = await _productRepo.Query().CountAsync(p => productIds.Contains(p.Id), ct);
        if (existing != productIds.Count)
            return ApiResponse<long>.Fail("One or more products not found.");

        var subtotal = cmd.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var vatAmount = Math.Round(subtotal * cmd.VatRate, 4, MidpointRounding.AwayFromZero);
        var total = subtotal + vatAmount;

        var code = await _numbering.NextAsync("PFM", null, ct);
        var entity = new Domain.Entities.ProformaInvoice
        {
            Code = code,
            CustomerId = cmd.CustomerId,
            SalesOrderId = cmd.SalesOrderId,
            IssueDate = cmd.IssueDate,
            ValidUntil = cmd.ValidUntil,
            Status = ProformaInvoiceStatus.Draft,
            CurrencyId = cmd.CurrencyId,
            ExchangeRate = cmd.ExchangeRate,
            VatRate = cmd.VatRate,
            SubtotalAmount = subtotal,
            VatAmount = vatAmount,
            TotalAmount = total,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            Lines = cmd.Lines.Select((l, i) => new ProformaInvoiceLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Proforma invoice draft created.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Update (Draft only): full re-snapshot incl. lines
// ───────────────────────────────────────────────────────────────────────────
public sealed record UpdateProformaInvoiceCommand(
    long Id,
    DateOnly IssueDate,
    DateOnly ValidUntil,
    decimal VatRate,
    string? Notes,
    IReadOnlyList<ProformaInvoiceLineInput> Lines
) : IRequest<ApiResponse>;

public sealed class UpdateProformaInvoiceCommandValidator : AbstractValidator<UpdateProformaInvoiceCommand>
{
    public UpdateProformaInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.ValidUntil).GreaterThanOrEqualTo(x => x.IssueDate);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
    }
}

internal sealed class UpdateProformaInvoiceCommandHandler
    : IRequestHandler<UpdateProformaInvoiceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    private readonly IRepository<ProformaInvoiceLine, long> _lineRepo;
    private readonly IUnitOfWork _uow;

    public UpdateProformaInvoiceCommandHandler(
        IRepository<Domain.Entities.ProformaInvoice, long> repo,
        IRepository<ProformaInvoiceLine, long> lineRepo,
        IUnitOfWork uow)
    {
        _repo = repo; _lineRepo = lineRepo; _uow = uow;
    }

    public async Task<ApiResponse> Handle(UpdateProformaInvoiceCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.Query()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, ct);
        if (entity is null) return ApiResponse.Fail("Proforma invoice not found.");
        if (entity.Status != ProformaInvoiceStatus.Draft)
            return ApiResponse.Fail($"Cannot edit a {entity.Status} proforma invoice.");

        entity.IssueDate = cmd.IssueDate;
        entity.ValidUntil = cmd.ValidUntil;
        entity.VatRate = cmd.VatRate;
        entity.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();

        // Replace all lines (simple v1 approach — full re-snapshot)
        foreach (var oldLine in entity.Lines.ToList()) _lineRepo.Remove(oldLine);
        entity.Lines.Clear();
        foreach (var (l, i) in cmd.Lines.Select((l, i) => (l, i)))
        {
            entity.Lines.Add(new ProformaInvoiceLine
            {
                ProformaInvoiceId = entity.Id,
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                SortOrder = i,
                LineNotes = l.LineNotes
            });
        }

        var subtotal = cmd.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var vatAmount = Math.Round(subtotal * cmd.VatRate, 4, MidpointRounding.AwayFromZero);
        entity.SubtotalAmount = subtotal;
        entity.VatAmount = vatAmount;
        entity.TotalAmount = subtotal + vatAmount;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Proforma invoice updated.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Delete (Draft only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record DeleteProformaInvoiceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteProformaInvoiceCommandHandler
    : IRequestHandler<DeleteProformaInvoiceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteProformaInvoiceCommandHandler(IRepository<Domain.Entities.ProformaInvoice, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteProformaInvoiceCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Proforma invoice not found.");
        if (e.Status != ProformaInvoiceStatus.Draft)
            return ApiResponse.Fail($"Cannot delete a {e.Status} proforma invoice. Cancel it first.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Proforma invoice deleted.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Send / Accept / Expire / Cancel
// ───────────────────────────────────────────────────────────────────────────
public sealed record SendProformaInvoiceCommand(long Id) : IRequest<ApiResponse>;
public sealed record AcceptProformaInvoiceCommand(long Id) : IRequest<ApiResponse>;
public sealed record ExpireProformaInvoiceCommand(long Id) : IRequest<ApiResponse>;
public sealed record CancelProformaInvoiceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class ProformaInvoiceStateHandlers :
    IRequestHandler<SendProformaInvoiceCommand, ApiResponse>,
    IRequestHandler<AcceptProformaInvoiceCommand, ApiResponse>,
    IRequestHandler<ExpireProformaInvoiceCommand, ApiResponse>,
    IRequestHandler<CancelProformaInvoiceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public ProformaInvoiceStateHandlers(
        IRepository<Domain.Entities.ProformaInvoice, long> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _repo = repo; _uow = uow; _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(SendProformaInvoiceCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Proforma invoice not found.");
        if (e.Status != ProformaInvoiceStatus.Draft)
            return ApiResponse.Fail($"Cannot send a {e.Status} proforma invoice.");
        e.Status = ProformaInvoiceStatus.Sent;
        e.SentAt = DateTimeOffset.UtcNow;
        e.SentBy = _currentUser.UserName ?? "system";
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Proforma invoice {e.Code} marked as sent.");
    }

    public async Task<ApiResponse> Handle(AcceptProformaInvoiceCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Proforma invoice not found.");
        if (e.Status != ProformaInvoiceStatus.Sent)
            return ApiResponse.Fail($"Only Sent proforma invoices can be marked accepted (current: {e.Status}).");
        e.Status = ProformaInvoiceStatus.Accepted;
        e.AcceptedAt = DateTimeOffset.UtcNow;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Proforma invoice {e.Code} marked as accepted.");
    }

    public async Task<ApiResponse> Handle(ExpireProformaInvoiceCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Proforma invoice not found.");
        if (e.Status != ProformaInvoiceStatus.Sent && e.Status != ProformaInvoiceStatus.Accepted)
            return ApiResponse.Fail($"Cannot expire a {e.Status} proforma invoice.");
        e.Status = ProformaInvoiceStatus.Expired;
        e.ExpiredAt = DateTimeOffset.UtcNow;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Proforma invoice {e.Code} marked as expired.");
    }

    public async Task<ApiResponse> Handle(CancelProformaInvoiceCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Proforma invoice not found.");
        if (e.Status == ProformaInvoiceStatus.Converted)
            return ApiResponse.Fail("Cannot cancel a proforma that's already been converted to a customer invoice.");
        if (e.Status == ProformaInvoiceStatus.Cancelled)
            return ApiResponse.Fail("Proforma invoice is already cancelled.");
        e.Status = ProformaInvoiceStatus.Cancelled;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Proforma invoice {e.Code} cancelled.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Convert Accepted Proforma → real CustomerInvoice
// ───────────────────────────────────────────────────────────────────────────
public sealed record ConvertProformaToCustomerInvoiceCommand(
    long ProformaInvoiceId,
    long SalesOrderId,            // the SO the real invoice belongs to (required by CustomerInvoice)
    DateOnly InvoiceDate,
    DateOnly? DueDate
) : IRequest<ApiResponse<long>>;

internal sealed class ConvertProformaToCustomerInvoiceCommandHandler
    : IRequestHandler<ConvertProformaToCustomerInvoiceCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.ProformaInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ConvertProformaToCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.ProformaInvoice, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo; _uow = uow; _mediator = mediator;
    }

    public async Task<ApiResponse<long>> Handle(ConvertProformaToCustomerInvoiceCommand cmd, CancellationToken ct)
    {
        var pf = await _repo.Query()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProformaInvoiceId, ct);
        if (pf is null) return ApiResponse<long>.Fail("Proforma invoice not found.");
        if (pf.Status != ProformaInvoiceStatus.Accepted)
            return ApiResponse<long>.Fail($"Only Accepted proforma invoices can be converted (current: {pf.Status}).");
        if (pf.ConvertedCustomerInvoiceId.HasValue)
            return ApiResponse<long>.Fail("This proforma has already been converted.");

        var lines = pf.Lines.OrderBy(l => l.SortOrder)
            .Select(l => new CustomerInvoiceLineInput(l.ProductId, l.Quantity, l.UnitPrice, l.LineNotes))
            .ToList();

        var result = await _mediator.Send(new CreateCustomerInvoiceCommand(
            cmd.SalesOrderId, pf.VatRate, cmd.InvoiceDate, cmd.DueDate,
            $"Converted from Proforma {pf.Code}",
            lines), ct);

        if (!result.Success || result.Data is null)
            return ApiResponse<long>.Fail(result.Message ?? "Failed to create customer invoice from proforma.");

        pf.Status = ProformaInvoiceStatus.Converted;
        pf.ConvertedCustomerInvoiceId = result.Data.Id;
        _repo.Update(pf);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(result.Data.Id,
            $"Proforma {pf.Code} converted to invoice {result.Data.Code}.");
    }
}
