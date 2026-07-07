using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Costing;

// ═══════════════════════════ DTO ═══════════════════════════

public sealed record CostingRateDto(
    int Id, string RateType, string Basis, decimal Rate, DateOnly EffectiveFrom,
    int? WorkCenterId, string? WorkCenterName, bool IsActive, string? Notes);

// ═══════════════════════════ Query ═══════════════════════════

public sealed record GetCostingRatesQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<CostingRateDto>>>;

internal sealed class GetCostingRatesQueryHandler
    : IRequestHandler<GetCostingRatesQuery, ApiResponse<IReadOnlyList<CostingRateDto>>>
{
    private readonly IRepository<CostingRate> _repo;
    public GetCostingRatesQueryHandler(IRepository<CostingRate> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<CostingRateDto>>> Handle(GetCostingRatesQuery q, CancellationToken ct)
    {
        IQueryable<CostingRate> query = _repo.Query().AsNoTracking().Include(r => r.WorkCenter);
        if (!q.IncludeInactive) query = query.Where(r => r.IsActive);

        var rows = await query
            .OrderBy(r => r.RateType).ThenByDescending(r => r.EffectiveFrom)
            .Select(r => new CostingRateDto(
                r.Id, r.RateType.ToString(), r.Basis.ToString(), r.Rate, r.EffectiveFrom,
                r.WorkCenterId, r.WorkCenter != null ? r.WorkCenter.Name : null, r.IsActive, r.Notes))
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<CostingRateDto>>.Ok(rows);
    }
}

// ═══════════════════════════ Create ═══════════════════════════

public sealed record CreateCostingRateCommand(
    string RateType, string Basis, decimal Rate, DateOnly EffectiveFrom, int? WorkCenterId, string? Notes)
    : IRequest<ApiResponse<int>>;

public sealed class CreateCostingRateCommandValidator : AbstractValidator<CreateCostingRateCommand>
{
    public CreateCostingRateCommandValidator()
    {
        RuleFor(x => x.RateType).Must(t => Enum.TryParse<CostingRateType>(t, out _))
            .WithMessage("RateType must be Labour, MachineOH or FactoryOH.");
        RuleFor(x => x.Basis).Must(b => Enum.TryParse<CostingRateBasis>(b, out _))
            .WithMessage("Basis must be PerLabourMinute, PerMachineHour or PerUnit.");
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

internal sealed class CreateCostingRateCommandHandler : IRequestHandler<CreateCostingRateCommand, ApiResponse<int>>
{
    private readonly IRepository<CostingRate> _repo;
    private readonly IUnitOfWork _uow;
    public CreateCostingRateCommandHandler(IRepository<CostingRate> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateCostingRateCommand cmd, CancellationToken ct)
    {
        var e = new CostingRate
        {
            RateType = Enum.Parse<CostingRateType>(cmd.RateType),
            Basis = Enum.Parse<CostingRateBasis>(cmd.Basis),
            Rate = cmd.Rate,
            EffectiveFrom = cmd.EffectiveFrom,
            WorkCenterId = cmd.WorkCenterId,
            IsActive = true,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Costing rate saved.");
    }
}

// ═══════════════════════════ Update ═══════════════════════════

public sealed record UpdateCostingRateCommand(
    int Id, string RateType, string Basis, decimal Rate, DateOnly EffectiveFrom,
    int? WorkCenterId, bool IsActive, string? Notes) : IRequest<ApiResponse>;

public sealed class UpdateCostingRateCommandValidator : AbstractValidator<UpdateCostingRateCommand>
{
    public UpdateCostingRateCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.RateType).Must(t => Enum.TryParse<CostingRateType>(t, out _));
        RuleFor(x => x.Basis).Must(b => Enum.TryParse<CostingRateBasis>(b, out _));
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

internal sealed class UpdateCostingRateCommandHandler : IRequestHandler<UpdateCostingRateCommand, ApiResponse>
{
    private readonly IRepository<CostingRate> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateCostingRateCommandHandler(IRepository<CostingRate> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateCostingRateCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Costing rate not found.");
        e.RateType = Enum.Parse<CostingRateType>(cmd.RateType);
        e.Basis = Enum.Parse<CostingRateBasis>(cmd.Basis);
        e.Rate = cmd.Rate;
        e.EffectiveFrom = cmd.EffectiveFrom;
        e.WorkCenterId = cmd.WorkCenterId;
        e.IsActive = cmd.IsActive;
        e.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Costing rate updated.");
    }
}
