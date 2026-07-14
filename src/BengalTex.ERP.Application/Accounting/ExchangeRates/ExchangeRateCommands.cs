using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.ExchangeRates;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record ExchangeRateDto(
    int Id, int CurrencyId, string CurrencyCode, DateOnly RateDate, decimal Rate, string? Source);

// ═══════════════════════════ List ═══════════════════════════

public sealed record GetExchangeRatesQuery(int? CurrencyId = null)
    : IRequest<ApiResponse<IReadOnlyList<ExchangeRateDto>>>;

internal sealed class GetExchangeRatesQueryHandler
    : IRequestHandler<GetExchangeRatesQuery, ApiResponse<IReadOnlyList<ExchangeRateDto>>>
{
    private readonly IRepository<ExchangeRate> _repo;
    public GetExchangeRatesQueryHandler(IRepository<ExchangeRate> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<ExchangeRateDto>>> Handle(GetExchangeRatesQuery q, CancellationToken ct)
    {
        var query = _repo.Query().AsNoTracking();
        if (q.CurrencyId.HasValue) query = query.Where(r => r.CurrencyId == q.CurrencyId.Value);

        var rows = await query
            .OrderByDescending(r => r.RateDate).ThenBy(r => r.Currency.Code)
            .Select(r => new ExchangeRateDto(r.Id, r.CurrencyId, r.Currency.Code, r.RateDate, r.Rate, r.Source))
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<ExchangeRateDto>>.Ok(rows);
    }
}

// ═══════════════════════════ Resolve as-of ═══════════════════════════

public sealed record GetRateAsOfQuery(int CurrencyId, DateOnly Date) : IRequest<ApiResponse<decimal>>;

internal sealed class GetRateAsOfQueryHandler : IRequestHandler<GetRateAsOfQuery, ApiResponse<decimal>>
{
    private readonly IExchangeRateResolver _resolver;
    public GetRateAsOfQueryHandler(IExchangeRateResolver resolver) => _resolver = resolver;

    public async Task<ApiResponse<decimal>> Handle(GetRateAsOfQuery q, CancellationToken ct)
        => ApiResponse<decimal>.Ok(await _resolver.GetRateAsOfAsync(q.CurrencyId, q.Date, ct));
}

// ═══════════════════════════ Upsert a dated rate ═══════════════════════════

public sealed record SetExchangeRateCommand(int CurrencyId, DateOnly RateDate, decimal Rate, string? Source)
    : IRequest<ApiResponse<int>>;

public sealed class SetExchangeRateCommandValidator : AbstractValidator<SetExchangeRateCommand>
{
    public SetExchangeRateCommandValidator()
    {
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.RateDate).NotEmpty();
        RuleFor(x => x.Rate).GreaterThan(0);
        RuleFor(x => x.Source).MaximumLength(100);
    }
}

internal sealed class SetExchangeRateCommandHandler : IRequestHandler<SetExchangeRateCommand, ApiResponse<int>>
{
    private readonly IRepository<ExchangeRate> _repo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IUnitOfWork _uow;

    public SetExchangeRateCommandHandler(IRepository<ExchangeRate> repo, IRepository<Domain.Entities.Currency> currencyRepo, IUnitOfWork uow)
    {
        _repo = repo; _currencyRepo = currencyRepo; _uow = uow;
    }

    public async Task<ApiResponse<int>> Handle(SetExchangeRateCommand cmd, CancellationToken ct)
    {
        if (!await _currencyRepo.Query().AnyAsync(c => c.Id == cmd.CurrencyId, ct))
            return ApiResponse<int>.Fail("Currency not found.");

        var rate = Math.Round(cmd.Rate, 6, MidpointRounding.AwayFromZero);
        // One rate per currency per date — update in place if the day already has one.
        var existing = await _repo.Query()
            .FirstOrDefaultAsync(r => r.CurrencyId == cmd.CurrencyId && r.RateDate == cmd.RateDate, ct);
        if (existing is not null)
        {
            existing.Rate = rate;
            existing.Source = string.IsNullOrWhiteSpace(cmd.Source) ? null : cmd.Source.Trim();
            _repo.Update(existing);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<int>.Ok(existing.Id, "Exchange rate updated.");
        }

        var entity = new ExchangeRate
        {
            CurrencyId = cmd.CurrencyId,
            RateDate = cmd.RateDate,
            Rate = rate,
            Source = string.IsNullOrWhiteSpace(cmd.Source) ? null : cmd.Source.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(entity.Id, "Exchange rate recorded.");
    }
}
