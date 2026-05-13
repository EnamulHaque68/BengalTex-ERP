using BengalTex.ERP.Application.Currency.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Currency.Commands;

public sealed record CreateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency
) : IRequest<ApiResponse<CurrencyDto>>;

public sealed class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(3)
            .Matches("^[A-Z]{3}$").WithMessage("Currency code must be 3 uppercase letters (ISO 4217).");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ExchangeRateToBase).GreaterThan(0);
    }
}

internal sealed class CreateCurrencyCommandHandler
    : IRequestHandler<CreateCurrencyCommand, ApiResponse<CurrencyDto>>
{
    private readonly IRepository<Domain.Entities.Currency> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CreateCurrencyCommandHandler(
        IRepository<Domain.Entities.Currency> repo,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CurrencyDto>> Handle(
        CreateCurrencyCommand cmd, CancellationToken cancellationToken)
    {
        var code = cmd.Code.ToUpperInvariant();

        if (await _repo.AnyAsync(c => c.Code == code, cancellationToken))
            return ApiResponse<CurrencyDto>.Fail($"Currency code '{code}' already exists.");

        // Single-base rule: demote any existing base FIRST and commit separately, otherwise
        // EF Core batches the demote-UPDATE and new-base-INSERT together and the SQL Server
        // filtered unique index (UX_Currencies_SingleBase) trips during the intermediate state.
        if (cmd.IsBaseCurrency)
        {
            var existingBase = await _repo.Query()
                .FirstOrDefaultAsync(c => c.IsBaseCurrency, cancellationToken);
            if (existingBase is not null)
            {
                existingBase.IsBaseCurrency = false;
                _repo.Update(existingBase);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }

        var entity = new Domain.Entities.Currency
        {
            Code = code,
            Name = cmd.Name,
            Symbol = cmd.Symbol,
            ExchangeRateToBase = cmd.IsBaseCurrency ? 1m : cmd.ExchangeRateToBase,
            IsBaseCurrency = cmd.IsBaseCurrency,
            IsActive = true
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<CurrencyDto>.Ok(_mapper.Map<CurrencyDto>(entity), "Currency created.");
    }
}
