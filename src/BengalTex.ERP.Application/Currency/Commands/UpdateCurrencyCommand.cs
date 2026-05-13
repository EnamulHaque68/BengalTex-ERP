using BengalTex.ERP.Application.Currency.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Currency.Commands;

public sealed record UpdateCurrencyCommand(
    int Id,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    bool IsActive
) : IRequest<ApiResponse<CurrencyDto>>;

public sealed class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ExchangeRateToBase).GreaterThan(0);
    }
}

internal sealed class UpdateCurrencyCommandHandler
    : IRequestHandler<UpdateCurrencyCommand, ApiResponse<CurrencyDto>>
{
    private readonly IRepository<Domain.Entities.Currency> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateCurrencyCommandHandler(
        IRepository<Domain.Entities.Currency> repo,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _repo = repo;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CurrencyDto>> Handle(
        UpdateCurrencyCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse<CurrencyDto>.Fail("Currency not found.");

        // Promoting another currency to base — demote the existing one FIRST and commit
        // separately, otherwise EF Core might batch both UPDATEs and the SQL Server filtered
        // unique index (UX_Currencies_SingleBase) trips during the intermediate "two-bases" state.
        if (cmd.IsBaseCurrency && !entity.IsBaseCurrency)
        {
            var existingBase = await _repo.Query()
                .FirstOrDefaultAsync(c => c.IsBaseCurrency && c.Id != cmd.Id, cancellationToken);
            if (existingBase is not null)
            {
                existingBase.IsBaseCurrency = false;
                _repo.Update(existingBase);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }

        entity.Name = cmd.Name;
        entity.Symbol = cmd.Symbol;
        entity.ExchangeRateToBase = cmd.IsBaseCurrency ? 1m : cmd.ExchangeRateToBase;
        entity.IsBaseCurrency = cmd.IsBaseCurrency;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<CurrencyDto>.Ok(_mapper.Map<CurrencyDto>(entity), "Currency updated.");
    }
}
