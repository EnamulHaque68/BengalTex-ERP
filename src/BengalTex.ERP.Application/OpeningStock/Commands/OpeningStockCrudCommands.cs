using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.OpeningStock.Dtos;
using BengalTex.ERP.Application.OpeningStock.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.OpeningStock.Commands;

// ── Shared line validation ──
internal static class OpeningStockRules
{
    public static void Lines<T>(AbstractValidator<T> v, Func<T, IReadOnlyList<OpeningStockLineInput>> sel)
    {
        v.RuleFor(x => sel(x)).NotEmpty().WithMessage("Add at least one line.");
        v.RuleForEach(x => sel(x)).ChildRules(l =>
        {
            l.RuleFor(i => i.ItemType).Must(t => t is "RawMaterial" or "Product")
                .WithMessage("ItemType must be 'RawMaterial' or 'Product'.");
            l.RuleFor(i => i.ItemId).GreaterThan(0);
            l.RuleFor(i => i.Quantity).GreaterThan(0);
            l.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0);
            l.RuleFor(i => i.LineNotes).MaximumLength(1000);
        });
    }
}

// ── Create ──
public sealed record CreateOpeningStockCommand(
    DateOnly EntryDate, int WarehouseId, string? Notes, IReadOnlyList<OpeningStockLineInput> Lines)
    : IRequest<ApiResponse<OpeningStockEntryDto>>;

public sealed class CreateOpeningStockCommandValidator : AbstractValidator<CreateOpeningStockCommand>
{
    public CreateOpeningStockCommandValidator()
    {
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        OpeningStockRules.Lines(this, x => x.Lines);
    }
}

internal sealed class CreateOpeningStockCommandHandler
    : IRequestHandler<CreateOpeningStockCommand, ApiResponse<OpeningStockEntryDto>>
{
    private readonly IRepository<OpeningStockEntry, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateOpeningStockCommandHandler(
        IRepository<OpeningStockEntry, long> repo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rmRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow, INumberingService numbering, IMediator mediator)
    {
        _repo = repo; _warehouseRepo = warehouseRepo; _rmRepo = rmRepo;
        _productRepo = productRepo; _uow = uow; _numbering = numbering; _mediator = mediator;
    }

    public async Task<ApiResponse<OpeningStockEntryDto>> Handle(CreateOpeningStockCommand cmd, CancellationToken ct)
    {
        if (await _warehouseRepo.GetByIdAsync(cmd.WarehouseId, ct) is null)
            return ApiResponse<OpeningStockEntryDto>.Fail("Warehouse not found.");

        var dupe = cmd.Lines.GroupBy(l => (l.ItemType, l.ItemId)).Any(g => g.Count() > 1);
        if (dupe) return ApiResponse<OpeningStockEntryDto>.Fail("The same item appears more than once — combine the quantities.");

        var entity = new OpeningStockEntry
        {
            Code = await _numbering.NextAsync("OS", null, ct),
            EntryDate = cmd.EntryDate,
            WarehouseId = cmd.WarehouseId,
            Status = OpeningStockStatus.Draft,
            Notes = cmd.Notes,
            Lines = new List<OpeningStockLine>()
        };

        var i = 0;
        foreach (var line in cmd.Lines)
        {
            var resolved = await ResolveLineAsync(line, ct);
            if (resolved is not null) return ApiResponse<OpeningStockEntryDto>.Fail(resolved);
            entity.Lines.Add(BuildLine(line, i++));
        }

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetOpeningStockEntryByIdQuery(entity.Id), ct);
    }

    private async Task<string?> ResolveLineAsync(OpeningStockLineInput line, CancellationToken ct)
    {
        if (line.ItemType == "RawMaterial")
            return await _rmRepo.GetByIdAsync(line.ItemId, ct) is null ? $"Raw material {line.ItemId} not found." : null;
        return await _productRepo.GetByIdAsync(line.ItemId, ct) is null ? $"Product {line.ItemId} not found." : null;
    }

    internal static OpeningStockLine BuildLine(OpeningStockLineInput line, int sort) => new()
    {
        RawMaterialId = line.ItemType == "RawMaterial" ? line.ItemId : null,
        ProductId = line.ItemType == "Product" ? line.ItemId : null,
        Quantity = line.Quantity,
        UnitCost = line.UnitCost,
        SortOrder = sort,
        LineNotes = string.IsNullOrWhiteSpace(line.LineNotes) ? null : line.LineNotes.Trim()
    };
}

// ── Update (draft only) ──
public sealed record UpdateOpeningStockCommand(
    long Id, DateOnly EntryDate, int WarehouseId, string? Notes, IReadOnlyList<OpeningStockLineInput> Lines)
    : IRequest<ApiResponse<OpeningStockEntryDto>>;

public sealed class UpdateOpeningStockCommandValidator : AbstractValidator<UpdateOpeningStockCommand>
{
    public UpdateOpeningStockCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        OpeningStockRules.Lines(this, x => x.Lines);
    }
}

internal sealed class UpdateOpeningStockCommandHandler
    : IRequestHandler<UpdateOpeningStockCommand, ApiResponse<OpeningStockEntryDto>>
{
    private readonly IRepository<OpeningStockEntry, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateOpeningStockCommandHandler(
        IRepository<OpeningStockEntry, long> repo, IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _warehouseRepo = warehouseRepo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<OpeningStockEntryDto>> Handle(UpdateOpeningStockCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (e is null) return ApiResponse<OpeningStockEntryDto>.Fail("Opening stock entry not found.");
        if (e.Status != OpeningStockStatus.Draft)
            return ApiResponse<OpeningStockEntryDto>.Fail("Only draft entries can be edited.");
        if (await _warehouseRepo.GetByIdAsync(cmd.WarehouseId, ct) is null)
            return ApiResponse<OpeningStockEntryDto>.Fail("Warehouse not found.");
        if (cmd.Lines.GroupBy(l => (l.ItemType, l.ItemId)).Any(g => g.Count() > 1))
            return ApiResponse<OpeningStockEntryDto>.Fail("The same item appears more than once — combine the quantities.");

        e.EntryDate = cmd.EntryDate;
        e.WarehouseId = cmd.WarehouseId;
        e.Notes = cmd.Notes;
        e.Lines.Clear();
        var i = 0;
        foreach (var line in cmd.Lines) e.Lines.Add(CreateOpeningStockCommandHandler.BuildLine(line, i++));

        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetOpeningStockEntryByIdQuery(e.Id), ct);
    }
}

// ── Delete (draft only) ──
public sealed record DeleteOpeningStockCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteOpeningStockCommandHandler : IRequestHandler<DeleteOpeningStockCommand, ApiResponse>
{
    private readonly IRepository<OpeningStockEntry, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteOpeningStockCommandHandler(IRepository<OpeningStockEntry, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteOpeningStockCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Opening stock entry not found.");
        if (e.Status != OpeningStockStatus.Draft)
            return ApiResponse.Fail("Only draft entries can be deleted.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Opening stock entry deleted.");
    }
}
