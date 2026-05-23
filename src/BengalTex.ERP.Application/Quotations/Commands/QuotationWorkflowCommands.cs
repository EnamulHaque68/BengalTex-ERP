using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Quotations.Dtos;
using BengalTex.ERP.Application.Quotations.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Quotations.Commands;

// ─── Send (Draft → Sent) ─────────────────────────────────────────────────────
public sealed record SendQuotationCommand(long Id) : IRequest<ApiResponse<QuotationDto>>;

internal sealed class SendQuotationCommandHandler : IRequestHandler<SendQuotationCommand, ApiResponse<QuotationDto>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    public SendQuotationCommandHandler(IRepository<Domain.Entities.Quotation, long> repo, IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<QuotationDto>> Handle(SendQuotationCommand cmd, CancellationToken ct)
    {
        var q = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (q is null) return ApiResponse<QuotationDto>.Fail("Quotation not found.");
        if (q.Status != QuotationStatus.Draft) return ApiResponse<QuotationDto>.Fail("Only draft quotations can be sent.");
        if (q.Lines.Count == 0) return ApiResponse<QuotationDto>.Fail("Cannot send a quotation with no lines.");

        q.Status = QuotationStatus.Sent;
        q.SentAt = DateTimeOffset.UtcNow;
        _repo.Update(q);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetQuotationByIdQuery(q.Id), ct);
    }
}

// ─── Decide (Sent → Accepted | Rejected) ─────────────────────────────────────
public sealed record DecideQuotationCommand(long Id, bool Accept) : IRequest<ApiResponse<QuotationDto>>;

internal sealed class DecideQuotationCommandHandler : IRequestHandler<DecideQuotationCommand, ApiResponse<QuotationDto>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;
    public DecideQuotationCommandHandler(IRepository<Domain.Entities.Quotation, long> repo, IUnitOfWork uow, ICurrentUserService currentUser, IMediator mediator)
    { _repo = repo; _uow = uow; _currentUser = currentUser; _mediator = mediator; }

    public async Task<ApiResponse<QuotationDto>> Handle(DecideQuotationCommand cmd, CancellationToken ct)
    {
        var q = await _repo.GetByIdAsync(cmd.Id, ct);
        if (q is null) return ApiResponse<QuotationDto>.Fail("Quotation not found.");
        if (q.Status != QuotationStatus.Sent)
            return ApiResponse<QuotationDto>.Fail("Only sent quotations can be accepted or rejected.");

        q.Status = cmd.Accept ? QuotationStatus.Accepted : QuotationStatus.Rejected;
        q.DecidedAt = DateTimeOffset.UtcNow;
        q.DecidedBy = _currentUser.UserName;
        _repo.Update(q);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetQuotationByIdQuery(q.Id), ct);
    }
}

// ─── Delete (Draft only) ─────────────────────────────────────────────────────
public sealed record DeleteQuotationCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteQuotationCommandHandler : IRequestHandler<DeleteQuotationCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteQuotationCommandHandler(IRepository<Domain.Entities.Quotation, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteQuotationCommand cmd, CancellationToken ct)
    {
        var q = await _repo.GetByIdAsync(cmd.Id, ct);
        if (q is null) return ApiResponse.Fail("Quotation not found.");
        if (q.Status != QuotationStatus.Draft) return ApiResponse.Fail("Only draft quotations can be deleted.");
        _repo.Remove(q);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Quotation deleted.");
    }
}

// ─── Revise (clone into a new Draft version) ─────────────────────────────────
public sealed record ReviseQuotationCommand(long Id) : IRequest<ApiResponse<QuotationDto>>;

internal sealed class ReviseQuotationCommandHandler : IRequestHandler<ReviseQuotationCommand, ApiResponse<QuotationDto>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;
    public ReviseQuotationCommandHandler(IRepository<Domain.Entities.Quotation, long> repo, IUnitOfWork uow, INumberingService numbering, IMediator mediator)
    { _repo = repo; _uow = uow; _numbering = numbering; _mediator = mediator; }

    public async Task<ApiResponse<QuotationDto>> Handle(ReviseQuotationCommand cmd, CancellationToken ct)
    {
        var src = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (src is null) return ApiResponse<QuotationDto>.Fail("Quotation not found.");
        if (src.Status == QuotationStatus.Converted)
            return ApiResponse<QuotationDto>.Fail("A converted quotation cannot be revised.");

        var clone = new Domain.Entities.Quotation
        {
            Code = await _numbering.NextAsync("QUOT", null, ct),
            CustomerId = src.CustomerId,
            QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidUntil = src.ValidUntil,
            CurrencyId = src.CurrencyId,
            ExchangeRate = src.ExchangeRate,
            Status = QuotationStatus.Draft,
            RevisionOfId = src.Id,
            Version = src.Version + 1,
            CustomerReference = src.CustomerReference,
            Notes = src.Notes,
            TotalAmount = src.TotalAmount,
            Lines = src.Lines.OrderBy(l => l.SortOrder).Select(l => new QuotationLine
            {
                ProductId = l.ProductId, Description = l.Description, Quantity = l.Quantity,
                MaterialCost = l.MaterialCost, LaborCost = l.LaborCost, MachineCost = l.MachineCost,
                OverheadCost = l.OverheadCost, WastagePercent = l.WastagePercent, MarginPercent = l.MarginPercent,
                UnitCost = l.UnitCost, UnitPrice = l.UnitPrice, LineTotal = l.LineTotal, SortOrder = l.SortOrder
            }).ToList()
        };

        await _repo.AddAsync(clone, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetQuotationByIdQuery(clone.Id), ct);
    }
}
