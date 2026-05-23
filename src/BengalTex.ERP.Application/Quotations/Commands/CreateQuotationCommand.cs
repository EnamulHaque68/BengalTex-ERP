using BengalTex.ERP.Application.Quotations.Dtos;
using BengalTex.ERP.Application.Quotations.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Quotations.Commands;

/// <summary>One quoted line with its per-unit cost breakdown (price is computed, not supplied).</summary>
public sealed record QuotationLineInput(
    int ProductId,
    string? Description,
    decimal Quantity,
    decimal MaterialCost,
    decimal LaborCost,
    decimal MachineCost,
    decimal OverheadCost,
    decimal WastagePercent,
    decimal MarginPercent);

public sealed record CreateQuotationCommand(
    int CustomerId,
    DateOnly QuotationDate,
    DateOnly? ValidUntil,
    int CurrencyId,
    decimal ExchangeRate,
    string? CustomerReference,
    string? Notes,
    IReadOnlyList<QuotationLineInput> Lines
) : IRequest<ApiResponse<QuotationDto>>;

public sealed class CreateQuotationCommandValidator : AbstractValidator<CreateQuotationCommand>
{
    public CreateQuotationCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
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
            line.RuleFor(l => l.MaterialCost).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LaborCost).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.MachineCost).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.OverheadCost).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.WastagePercent).InclusiveBetween(0, 100);
            line.RuleFor(l => l.MarginPercent).GreaterThanOrEqualTo(0);
        });
    }
}

internal sealed class CreateQuotationCommandHandler
    : IRequestHandler<CreateQuotationCommand, ApiResponse<QuotationDto>>
{
    private readonly IRepository<Domain.Entities.Quotation, long> _repo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateQuotationCommandHandler(
        IRepository<Domain.Entities.Quotation, long> repo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _currencyRepo = currencyRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QuotationDto>> Handle(CreateQuotationCommand cmd, CancellationToken ct)
    {
        if (await _customerRepo.GetByIdAsync(cmd.CustomerId, ct) is null)
            return ApiResponse<QuotationDto>.Fail("Customer not found.");
        if (await _currencyRepo.GetByIdAsync(cmd.CurrencyId, ct) is null)
            return ApiResponse<QuotationDto>.Fail("Currency not found.");

        var entity = new Domain.Entities.Quotation
        {
            Code = await _numbering.NextAsync("QUOT", null, ct),
            CustomerId = cmd.CustomerId,
            QuotationDate = cmd.QuotationDate,
            ValidUntil = cmd.ValidUntil,
            CurrencyId = cmd.CurrencyId,
            ExchangeRate = cmd.ExchangeRate,
            Status = QuotationStatus.Draft,
            Version = 1,
            CustomerReference = string.IsNullOrWhiteSpace(cmd.CustomerReference) ? null : cmd.CustomerReference.Trim(),
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            Lines = cmd.Lines.Select((l, i) => BuildLine(l, i)).ToList()
        };
        entity.TotalAmount = Math.Round(entity.Lines.Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetQuotationByIdQuery(entity.Id), ct);
    }

    internal static QuotationLine BuildLine(QuotationLineInput l, int sortOrder)
    {
        var line = new QuotationLine
        {
            ProductId = l.ProductId,
            Description = string.IsNullOrWhiteSpace(l.Description) ? null : l.Description.Trim(),
            Quantity = l.Quantity,
            MaterialCost = l.MaterialCost,
            LaborCost = l.LaborCost,
            MachineCost = l.MachineCost,
            OverheadCost = l.OverheadCost,
            WastagePercent = l.WastagePercent,
            MarginPercent = l.MarginPercent,
            SortOrder = sortOrder
        };
        QuotationCosting.Compute(line);
        return line;
    }
}
