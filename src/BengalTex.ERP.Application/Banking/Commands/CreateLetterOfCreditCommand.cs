using BengalTex.ERP.Application.Banking.Dtos;
using BengalTex.ERP.Application.Banking.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Banking.Commands;

public sealed record CreateLetterOfCreditCommand(
    string? Code,
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

public sealed class CreateLetterOfCreditCommandValidator : AbstractValidator<CreateLetterOfCreditCommand>
{
    public CreateLetterOfCreditCommandValidator()
    {
        RuleFor(x => x.LcNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.IssuingBank).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.IssueDate).NotEmpty();
        RuleFor(x => x.ExpiryDate).NotEmpty()
            .GreaterThanOrEqualTo(x => x.IssueDate).WithMessage("Expiry date must be on or after the issue date.");
        RuleFor(x => x.TenorDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateLetterOfCreditCommandHandler
    : IRequestHandler<CreateLetterOfCreditCommand, ApiResponse<LetterOfCreditDto>>
{
    private readonly IRepository<LetterOfCredit, long> _repo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateLetterOfCreditCommandHandler(
        IRepository<LetterOfCredit, long> repo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IUnitOfWork uow, INumberingService numbering, IMediator mediator)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _currencyRepo = currencyRepo;
        _poRepo = poRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<LetterOfCreditDto>> Handle(CreateLetterOfCreditCommand cmd, CancellationToken ct)
    {
        if (await _supplierRepo.GetByIdAsync(cmd.SupplierId, ct) is null)
            return ApiResponse<LetterOfCreditDto>.Fail("Supplier not found.");
        if (await _currencyRepo.GetByIdAsync(cmd.CurrencyId, ct) is null)
            return ApiResponse<LetterOfCreditDto>.Fail("Currency not found.");
        if (cmd.PurchaseOrderId.HasValue && await _poRepo.GetByIdAsync(cmd.PurchaseOrderId.Value, ct) is null)
            return ApiResponse<LetterOfCreditDto>.Fail("Purchase order not found.");

        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("LC", null, ct)
            : cmd.Code.Trim().ToUpperInvariant();
        if (await _repo.AnyAsync(l => l.Code == code, ct))
            return ApiResponse<LetterOfCreditDto>.Fail($"LC code '{code}' already exists.");

        var entity = new LetterOfCredit
        {
            Code = code,
            LcNumber = cmd.LcNumber.Trim(),
            IssuingBank = cmd.IssuingBank.Trim(),
            SupplierId = cmd.SupplierId,
            PurchaseOrderId = cmd.PurchaseOrderId,
            CurrencyId = cmd.CurrencyId,
            ExchangeRate = cmd.ExchangeRate,
            Amount = cmd.Amount,
            IssueDate = cmd.IssueDate,
            ExpiryDate = cmd.ExpiryDate,
            TenorDays = cmd.TenorDays,
            Status = LcStatus.Draft,
            Notes = cmd.Notes
        };

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetLetterOfCreditByIdQuery(entity.Id), ct);
    }
}
