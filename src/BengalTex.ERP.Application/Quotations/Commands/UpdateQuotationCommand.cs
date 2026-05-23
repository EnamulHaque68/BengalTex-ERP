using BengalTex.ERP.Application.Quotations.Dtos;
using BengalTex.ERP.Application.Quotations.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Quotations.Commands;

public sealed record UpdateQuotationCommand(
    long Id,
    DateOnly QuotationDate,
    DateOnly? ValidUntil,
    int CurrencyId,
    decimal ExchangeRate,
    string? CustomerReference,
    string? Notes,
    IReadOnlyList<QuotationLineInput> Lines
) : IRequest<ApiResponse<QuotationDto>>;

public sealed class UpdateQuotationCommandValidator : AbstractValidator<UpdateQuotationCommand>
{
    public UpdateQuotationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.QuotationDate).NotEmpty();
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.CustomerReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A quotation needs at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Description).MaximumLength(500);
            line.RuleFor(l => l.WastagePercent).InclusiveBetween(0, 100);
            line.RuleFor(l => l.MarginPercent).GreaterThanOrEqualTo(0);
        });
    }
}

internal sealed class UpdateQuotationCommandHandler
    : IRequestHandler<UpdateQuotationCommand, ApiResponse<QuotationDto>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateQuotationCommandHandler(
        IRepository<Domain.Entities.Quotation, long> repo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _currencyRepo = currencyRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QuotationDto>> Handle(UpdateQuotationCommand cmd, CancellationToken ct)
    {
        var q = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (q is null) return ApiResponse<QuotationDto>.Fail("Quotation not found.");
        if (q.Status != QuotationStatus.Draft)
            return ApiResponse<QuotationDto>.Fail("Only draft quotations can be edited.");
        if (await _currencyRepo.GetByIdAsync(cmd.CurrencyId, ct) is null)
            return ApiResponse<QuotationDto>.Fail("Currency not found.");

        q.QuotationDate = cmd.QuotationDate;
        q.ValidUntil = cmd.ValidUntil;
        q.CurrencyId = cmd.CurrencyId;
        q.ExchangeRate = cmd.ExchangeRate;
        q.CustomerReference = string.IsNullOrWhiteSpace(cmd.CustomerReference) ? null : cmd.CustomerReference.Trim();
        q.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();

        q.Lines.Clear();
        var i = 0;
        foreach (var l in cmd.Lines) q.Lines.Add(CreateQuotationCommandHandler.BuildLine(l, i++));
        q.TotalAmount = Math.Round(q.Lines.Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);

        _repo.Update(q);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetQuotationByIdQuery(q.Id), ct);
    }
}
