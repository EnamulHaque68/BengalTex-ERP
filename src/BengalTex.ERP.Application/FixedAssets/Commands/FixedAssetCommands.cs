using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.FixedAssets.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.FixedAssets.Commands;

// ─── Helpers ───────────────────────────────────────────────────────────────
internal static class FixedAssetMapping
{
    public static decimal MonthlyDepreciation(FixedAsset a) =>
        a.UsefulLifeYears > 0
            ? Math.Round((a.AcquisitionCost - a.SalvageValue) / (a.UsefulLifeYears * 12m), 2, MidpointRounding.AwayFromZero)
            : 0m;

    public static FixedAssetDto ToDto(FixedAsset a) => new(
        a.Id, a.Code, a.Name, a.Category.ToString(), a.Location,
        a.MachineId, a.Machine?.Code,
        a.AcquisitionDate, a.AcquisitionCost, a.SalvageValue, a.UsefulLifeYears,
        a.DepreciationMethod.ToString(),
        a.AccumulatedDepreciation, a.GetNetBookValue(),
        MonthlyDepreciation(a),
        a.LastDepreciationYearMonth,
        a.Status.ToString(),
        a.DisposalDate, a.DisposalProceeds, a.DisposalNotes, a.DisposedByUser,
        a.Notes);
}

// ─── List ──────────────────────────────────────────────────────────────────
public sealed record GetFixedAssetsQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    string? Category = null
) : IRequest<ApiResponse<PagedResult<FixedAssetDto>>>;

internal sealed class GetFixedAssetsQueryHandler
    : IRequestHandler<GetFixedAssetsQuery, ApiResponse<PagedResult<FixedAssetDto>>>
{
    private readonly IRepository<FixedAsset, long> _repo;
    public GetFixedAssetsQueryHandler(IRepository<FixedAsset, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<FixedAssetDto>>> Handle(GetFixedAssetsQuery req, CancellationToken ct)
    {
        var q = _repo.Query().Include(a => a.Machine);
        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<FixedAssetStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s).Include(a => a.Machine);
        if (!string.IsNullOrEmpty(req.Category) && Enum.TryParse<FixedAssetCategory>(req.Category, out var c))
            q = q.Where(x => x.Category == c).Include(a => a.Machine);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search) || x.Name.Contains(search) ||
                             (x.Location != null && x.Location.Contains(search))).Include(a => a.Machine);

        var ordered = q.OrderByDescending(x => x.AcquisitionDate).ThenByDescending(x => x.Id);
        var total = await ordered.CountAsync(ct);
        var entities = await ordered
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .ToListAsync(ct);

        return ApiResponse<PagedResult<FixedAssetDto>>.Ok(
            PagedResult<FixedAssetDto>.Create(
                entities.Select(FixedAssetMapping.ToDto).ToList(),
                req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ─── Get By Id ─────────────────────────────────────────────────────────────
public sealed record GetFixedAssetByIdQuery(long Id) : IRequest<ApiResponse<FixedAssetDto>>;

internal sealed class GetFixedAssetByIdQueryHandler
    : IRequestHandler<GetFixedAssetByIdQuery, ApiResponse<FixedAssetDto>>
{
    private readonly IRepository<FixedAsset, long> _repo;
    public GetFixedAssetByIdQueryHandler(IRepository<FixedAsset, long> repo) => _repo = repo;

    public async Task<ApiResponse<FixedAssetDto>> Handle(GetFixedAssetByIdQuery q, CancellationToken ct)
    {
        var a = await _repo.Query().Include(x => x.Machine).FirstOrDefaultAsync(x => x.Id == q.Id, ct);
        return a is null
            ? ApiResponse<FixedAssetDto>.Fail("Fixed asset not found.")
            : ApiResponse<FixedAssetDto>.Ok(FixedAssetMapping.ToDto(a));
    }
}

// ─── Create ────────────────────────────────────────────────────────────────
public sealed record CreateFixedAssetCommand(
    string Name,
    string Category,
    string? Location,
    int? MachineId,
    DateOnly AcquisitionDate,
    decimal AcquisitionCost,
    decimal SalvageValue,
    int UsefulLifeYears,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateFixedAssetCommandValidator : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().Must(v => Enum.TryParse<FixedAssetCategory>(v, out _));
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.AcquisitionDate).NotEmpty();
        RuleFor(x => x.AcquisitionCost).GreaterThan(0);
        RuleFor(x => x.SalvageValue).GreaterThanOrEqualTo(0)
            .Must((cmd, sv) => sv < cmd.AcquisitionCost)
            .WithMessage("Salvage value must be less than acquisition cost.");
        RuleFor(x => x.UsefulLifeYears).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateFixedAssetCommandHandler
    : IRequestHandler<CreateFixedAssetCommand, ApiResponse<long>>
{
    private readonly IRepository<FixedAsset, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateFixedAssetCommandHandler(
        IRepository<FixedAsset, long> repo, IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateFixedAssetCommand cmd, CancellationToken ct)
    {
        var code = await _numbering.NextAsync("FA", null, ct);
        var entity = new FixedAsset
        {
            Code = code,
            Name = cmd.Name.Trim(),
            Category = Enum.Parse<FixedAssetCategory>(cmd.Category),
            Location = string.IsNullOrWhiteSpace(cmd.Location) ? null : cmd.Location.Trim(),
            MachineId = cmd.MachineId,
            AcquisitionDate = cmd.AcquisitionDate,
            AcquisitionCost = cmd.AcquisitionCost,
            SalvageValue = cmd.SalvageValue,
            UsefulLifeYears = cmd.UsefulLifeYears,
            DepreciationMethod = DepreciationMethod.StraightLine,
            AccumulatedDepreciation = 0m,
            Status = FixedAssetStatus.Active,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, $"Fixed asset {entity.Code} created.");
    }
}

// ─── Update (Active only; cannot edit cost once depreciation has run) ──────
public sealed record UpdateFixedAssetCommand(
    long Id,
    string Name,
    string Category,
    string? Location,
    int? MachineId,
    DateOnly AcquisitionDate,
    decimal AcquisitionCost,
    decimal SalvageValue,
    int UsefulLifeYears,
    string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateFixedAssetCommandValidator : AbstractValidator<UpdateFixedAssetCommand>
{
    public UpdateFixedAssetCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).Must(v => Enum.TryParse<FixedAssetCategory>(v, out _));
        RuleFor(x => x.AcquisitionCost).GreaterThan(0);
        RuleFor(x => x.SalvageValue).GreaterThanOrEqualTo(0)
            .Must((cmd, sv) => sv < cmd.AcquisitionCost);
        RuleFor(x => x.UsefulLifeYears).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateFixedAssetCommandHandler : IRequestHandler<UpdateFixedAssetCommand, ApiResponse>
{
    private readonly IRepository<FixedAsset, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateFixedAssetCommandHandler(IRepository<FixedAsset, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateFixedAssetCommand cmd, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(cmd.Id, ct);
        if (a is null) return ApiResponse.Fail("Fixed asset not found.");
        if (a.Status != FixedAssetStatus.Active) return ApiResponse.Fail($"Cannot edit a {a.Status} asset.");
        if (a.AccumulatedDepreciation > 0
            && (cmd.AcquisitionCost != a.AcquisitionCost
                || cmd.SalvageValue != a.SalvageValue
                || cmd.UsefulLifeYears != a.UsefulLifeYears
                || cmd.AcquisitionDate != a.AcquisitionDate))
            return ApiResponse.Fail("Cannot change cost/salvage/life/date once depreciation has been posted. Dispose and re-register if needed.");

        a.Name = cmd.Name.Trim();
        a.Category = Enum.Parse<FixedAssetCategory>(cmd.Category);
        a.Location = string.IsNullOrWhiteSpace(cmd.Location) ? null : cmd.Location.Trim();
        a.MachineId = cmd.MachineId;
        a.AcquisitionDate = cmd.AcquisitionDate;
        a.AcquisitionCost = cmd.AcquisitionCost;
        a.SalvageValue = cmd.SalvageValue;
        a.UsefulLifeYears = cmd.UsefulLifeYears;
        a.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(a);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Fixed asset updated.");
    }
}

// ─── Delete (only when no depreciation posted) ─────────────────────────────
public sealed record DeleteFixedAssetCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteFixedAssetCommandHandler : IRequestHandler<DeleteFixedAssetCommand, ApiResponse>
{
    private readonly IRepository<FixedAsset, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteFixedAssetCommandHandler(IRepository<FixedAsset, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteFixedAssetCommand cmd, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(cmd.Id, ct);
        if (a is null) return ApiResponse.Fail("Fixed asset not found.");
        if (a.AccumulatedDepreciation > 0)
            return ApiResponse.Fail("Cannot delete an asset with posted depreciation. Use Dispose instead.");
        if (a.Status != FixedAssetStatus.Active)
            return ApiResponse.Fail($"Cannot delete a {a.Status} asset.");
        _repo.Remove(a);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Fixed asset deleted.");
    }
}

// ─── Run Monthly Depreciation (batch + auto-journal) ───────────────────────
public sealed record RunDepreciationCommand(int Year, int Month) : IRequest<ApiResponse<long>>;

public sealed class RunDepreciationCommandValidator : AbstractValidator<RunDepreciationCommand>
{
    public RunDepreciationCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

internal sealed class RunDepreciationCommandHandler
    : IRequestHandler<RunDepreciationCommand, ApiResponse<long>>
{
    private readonly IRepository<FixedAsset, long> _assetRepo;
    private readonly IRepository<AssetDepreciationRun, long> _runRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;

    public RunDepreciationCommandHandler(
        IRepository<FixedAsset, long> assetRepo,
        IRepository<AssetDepreciationRun, long> runRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IJournalPostingService journal,
        ICurrentUserService currentUser)
    {
        _assetRepo = assetRepo; _runRepo = runRepo; _uow = uow;
        _numbering = numbering; _journal = journal; _currentUser = currentUser;
    }

    public async Task<ApiResponse<long>> Handle(RunDepreciationCommand cmd, CancellationToken ct)
    {
        // Already run for this month?
        var existing = await _runRepo.Query()
            .AnyAsync(r => r.Year == cmd.Year && r.Month == cmd.Month, ct);
        if (existing)
            return ApiResponse<long>.Fail($"Depreciation already posted for {cmd.Year:0000}-{cmd.Month:00}. Cannot post twice.");

        var ym = cmd.Year * 100 + cmd.Month;
        var monthStart = new DateOnly(cmd.Year, cmd.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // Eligible: Active assets, acquired on or before the month start, NOT yet fully depreciated,
        // and not already run for this YYYYMM.
        var assets = await _assetRepo.Query()
            .Where(a => a.Status == FixedAssetStatus.Active
                     && a.AcquisitionDate <= monthEnd
                     && a.AccumulatedDepreciation < (a.AcquisitionCost - a.SalvageValue)
                     && (a.LastDepreciationYearMonth == null || a.LastDepreciationYearMonth < ym))
            .ToListAsync(ct);

        if (assets.Count == 0)
            return ApiResponse<long>.Fail("No assets eligible for depreciation in this month.");

        var code = await _numbering.NextAsync("DEP", null, ct);
        var run = new AssetDepreciationRun
        {
            Code = code,
            Year = cmd.Year,
            Month = cmd.Month,
            RunDate = monthEnd,
            PostedByUser = _currentUser.UserName ?? "system",
            AssetCount = 0,
            TotalAmount = 0m
        };

        decimal grandTotal = 0m;
        foreach (var a in assets)
        {
            var monthly = FixedAssetMapping.MonthlyDepreciation(a);
            // Don't depreciate past (Cost - Salvage)
            var remaining = (a.AcquisitionCost - a.SalvageValue) - a.AccumulatedDepreciation;
            var thisRun = Math.Min(monthly, remaining);
            if (thisRun <= 0m) continue;

            a.AccumulatedDepreciation += thisRun;
            a.LastDepreciationYearMonth = ym;
            _assetRepo.Update(a);

            run.Lines.Add(new AssetDepreciationRunLine
            {
                FixedAssetId = a.Id,
                MonthlyDepreciation = thisRun,
                AccumulatedAfter = a.AccumulatedDepreciation,
                NetBookValueAfter = a.GetNetBookValue()
            });
            grandTotal += thisRun;
        }

        if (run.Lines.Count == 0)
            return ApiResponse<long>.Fail("No depreciation amounts to post (all eligible assets already fully depreciated).");

        run.AssetCount = run.Lines.Count;
        run.TotalAmount = grandTotal;
        await _runRepo.AddAsync(run, ct);
        await _uow.SaveChangesAsync(ct);

        // Auto-journal: Dr Depreciation Expense 5320 / Cr Accumulated Depreciation 1215
        await _journal.PostAsync(
            run.RunDate, $"Monthly depreciation {cmd.Year:0000}-{cmd.Month:00} ({run.Code})",
            "AssetDepreciationRun", run.Id, run.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.DepreciationExpense, grandTotal, 0m),
                new JournalPostingLine(LedgerAccounts.AccumulatedDepreciation, 0m, grandTotal)
            }, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(run.Id,
            $"Depreciation posted for {cmd.Year:0000}-{cmd.Month:00}: {run.AssetCount} asset(s), total ৳{grandTotal:N2}.");
    }
}

// ─── Dispose (sell / write-off) ────────────────────────────────────────────
public sealed record DisposeFixedAssetCommand(
    long Id,
    DateOnly DisposalDate,
    decimal DisposalProceeds,
    string? Notes,
    bool IsWriteOff = false
) : IRequest<ApiResponse>;

public sealed class DisposeFixedAssetCommandValidator : AbstractValidator<DisposeFixedAssetCommand>
{
    public DisposeFixedAssetCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DisposalDate).NotEmpty();
        RuleFor(x => x.DisposalProceeds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class DisposeFixedAssetCommandHandler : IRequestHandler<DisposeFixedAssetCommand, ApiResponse>
{
    private readonly IRepository<FixedAsset, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;

    public DisposeFixedAssetCommandHandler(
        IRepository<FixedAsset, long> repo, IUnitOfWork uow,
        IJournalPostingService journal, ICurrentUserService currentUser)
    {
        _repo = repo; _uow = uow; _journal = journal; _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(DisposeFixedAssetCommand cmd, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(cmd.Id, ct);
        if (a is null) return ApiResponse.Fail("Fixed asset not found.");
        if (a.Status != FixedAssetStatus.Active) return ApiResponse.Fail($"Asset is already {a.Status}.");

        var proceeds = cmd.IsWriteOff ? 0m : cmd.DisposalProceeds;
        var nbv = a.GetNetBookValue();
        var gainOrLoss = proceeds - nbv;   // +ve = gain, -ve = loss

        a.Status = cmd.IsWriteOff ? FixedAssetStatus.WrittenOff : FixedAssetStatus.Disposed;
        a.DisposalDate = cmd.DisposalDate;
        a.DisposalProceeds = proceeds;
        a.DisposalNotes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        a.DisposedByUser = _currentUser.UserName ?? "system";
        _repo.Update(a);
        await _uow.SaveChangesAsync(ct);

        // Auto-journal:
        //   Dr Cash/Bank (proceeds)                    [if proceeds > 0]
        //   Dr Accumulated Depreciation                [a.AccumulatedDepreciation]
        //   Dr Loss on Disposal                        [if proceeds < nbv]
        //          Cr Fixed Asset (Machinery 1210)     [a.AcquisitionCost]
        //          Cr Gain on Disposal                 [if proceeds > nbv]
        var lines = new List<JournalPostingLine>();
        if (proceeds > 0m)
            lines.Add(new JournalPostingLine(LedgerAccounts.Cash, proceeds, 0m));
        if (a.AccumulatedDepreciation > 0m)
            lines.Add(new JournalPostingLine(LedgerAccounts.AccumulatedDepreciation, a.AccumulatedDepreciation, 0m));
        if (gainOrLoss < 0m)
            lines.Add(new JournalPostingLine(LedgerAccounts.LossOnAssetDisposal, -gainOrLoss, 0m));

        lines.Add(new JournalPostingLine(LedgerAccounts.MachineryEquipment, 0m, a.AcquisitionCost));
        if (gainOrLoss > 0m)
            lines.Add(new JournalPostingLine(LedgerAccounts.GainOnAssetDisposal, 0m, gainOrLoss));

        await _journal.PostAsync(
            cmd.DisposalDate,
            $"Dispose Fixed Asset {a.Code} ({a.Name})" + (cmd.IsWriteOff ? " — write-off" : ""),
            cmd.IsWriteOff ? "FixedAssetWriteOff" : "FixedAssetDispose",
            a.Id, a.Code, lines, ct);
        await _uow.SaveChangesAsync(ct);

        var nature = cmd.IsWriteOff ? "written off"
                   : gainOrLoss > 0m ? $"disposed with gain ৳{gainOrLoss:N2}"
                   : gainOrLoss < 0m ? $"disposed with loss ৳{-gainOrLoss:N2}"
                   : "disposed at net book value";
        return ApiResponse.Ok($"Asset {a.Code} {nature}.");
    }
}

// ─── List depreciation runs + get run by id ────────────────────────────────
public sealed record GetDepreciationRunsQuery(PagedQueryParameters Parameters)
    : IRequest<ApiResponse<PagedResult<AssetDepreciationRunDto>>>;

internal sealed class GetDepreciationRunsQueryHandler
    : IRequestHandler<GetDepreciationRunsQuery, ApiResponse<PagedResult<AssetDepreciationRunDto>>>
{
    private readonly IRepository<AssetDepreciationRun, long> _repo;
    public GetDepreciationRunsQueryHandler(IRepository<AssetDepreciationRun, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<AssetDepreciationRunDto>>> Handle(GetDepreciationRunsQuery req, CancellationToken ct)
    {
        var q = _repo.Query().OrderByDescending(x => x.Year).ThenByDescending(x => x.Month);
        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(x => new AssetDepreciationRunDto(
                x.Id, x.Code, x.Year, x.Month, x.RunDate, x.TotalAmount, x.AssetCount,
                x.PostedByUser, x.Notes,
                new List<AssetDepreciationRunLineDto>()))
            .ToListAsync(ct);
        return ApiResponse<PagedResult<AssetDepreciationRunDto>>.Ok(
            PagedResult<AssetDepreciationRunDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

public sealed record GetDepreciationRunByIdQuery(long Id) : IRequest<ApiResponse<AssetDepreciationRunDto>>;

internal sealed class GetDepreciationRunByIdQueryHandler
    : IRequestHandler<GetDepreciationRunByIdQuery, ApiResponse<AssetDepreciationRunDto>>
{
    private readonly IRepository<AssetDepreciationRun, long> _repo;
    public GetDepreciationRunByIdQueryHandler(IRepository<AssetDepreciationRun, long> repo) => _repo = repo;

    public async Task<ApiResponse<AssetDepreciationRunDto>> Handle(GetDepreciationRunByIdQuery q, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(x => x.Id == q.Id)
            .Select(x => new AssetDepreciationRunDto(
                x.Id, x.Code, x.Year, x.Month, x.RunDate, x.TotalAmount, x.AssetCount,
                x.PostedByUser, x.Notes,
                x.Lines.Select(l => new AssetDepreciationRunLineDto(
                    l.Id, l.FixedAssetId, l.FixedAsset.Code, l.FixedAsset.Name,
                    l.MonthlyDepreciation, l.AccumulatedAfter, l.NetBookValueAfter)).ToList()))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? ApiResponse<AssetDepreciationRunDto>.Fail("Depreciation run not found.")
            : ApiResponse<AssetDepreciationRunDto>.Ok(dto);
    }
}
