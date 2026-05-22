using BengalTex.ERP.Application.Banking.Dtos;
using BengalTex.ERP.Application.Banking.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Banking.Commands;

/// <summary>Edits an LC's details while still Draft or Open.</summary>
public sealed record UpdateLetterOfCreditCommand(
    long Id,
    string LcNumber,
    string IssuingBank,
    int SupplierId,
    long? PurchaseOrderId,
    int CurrencyId,
    decimal ExchangeRate,
    decimal Amount,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    int TenorDays,
    string? Notes
) : IRequest<ApiResponse<LetterOfCreditDto>>;

public sealed class UpdateLetterOfCreditCommandValidator : AbstractValidator<UpdateLetterOfCreditCommand>
{
    public UpdateLetterOfCreditCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.LcNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.IssuingBank).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.IssueDate).NotEmpty();
        RuleFor(x => x.ExpiryDate).GreaterThanOrEqualTo(x => x.IssueDate);
        RuleFor(x => x.TenorDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateLetterOfCreditCommandHandler
    : IRequestHandler<UpdateLetterOfCreditCommand, ApiResponse<LetterOfCreditDto>>
{
    private readonly IRepository<LetterOfCredit, long> _repo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateLetterOfCreditCommandHandler(
        IRepository<LetterOfCredit, long> repo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _currencyRepo = currencyRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<LetterOfCreditDto>> Handle(UpdateLetterOfCreditCommand cmd, CancellationToken ct)
    {
        var lc = await _repo.GetByIdAsync(cmd.Id, ct);
        if (lc is null) return ApiResponse<LetterOfCreditDto>.Fail("Letter of credit not found.");
        if (lc.Status is not (LcStatus.Draft or LcStatus.Open))
            return ApiResponse<LetterOfCreditDto>.Fail("Only Draft or Open LCs can be edited.");
        if (await _supplierRepo.GetByIdAsync(cmd.SupplierId, ct) is null)
            return ApiResponse<LetterOfCreditDto>.Fail("Supplier not found.");
        if (await _currencyRepo.GetByIdAsync(cmd.CurrencyId, ct) is null)
            return ApiResponse<LetterOfCreditDto>.Fail("Currency not found.");

        lc.LcNumber = cmd.LcNumber.Trim();
        lc.IssuingBank = cmd.IssuingBank.Trim();
        lc.SupplierId = cmd.SupplierId;
        lc.PurchaseOrderId = cmd.PurchaseOrderId;
        lc.CurrencyId = cmd.CurrencyId;
        lc.ExchangeRate = cmd.ExchangeRate;
        lc.Amount = cmd.Amount;
        lc.IssueDate = cmd.IssueDate;
        lc.ExpiryDate = cmd.ExpiryDate;
        lc.TenorDays = cmd.TenorDays;
        lc.Notes = cmd.Notes;

        _repo.Update(lc);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetLetterOfCreditByIdQuery(lc.Id), ct);
    }
}
