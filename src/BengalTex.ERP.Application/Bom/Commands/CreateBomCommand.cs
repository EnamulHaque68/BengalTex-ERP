using BengalTex.ERP.Application.Bom.Dtos;
using BengalTex.ERP.Application.Bom.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Bom.Commands;

/// <summary>
/// One component line submitted with a create/update BOM request. Polymorphic:
/// <paramref name="ItemType"/> is "RawMaterial" or "Product" (sub-assembly), and
/// <paramref name="ItemId"/> is the corresponding RawMaterial or Product id.
/// </summary>
public sealed record BomLineInput(
    string ItemType,
    int ItemId,
    decimal Quantity,
    decimal WastagePercent,
    string? LineNotes);

public sealed record CreateBomCommand(
    int ProductId,
    string? Name,
    decimal OutputQuantity,
    DateOnly? EffectiveDate,
    string? Notes,
    IReadOnlyList<BomLineInput> Lines
) : IRequest<ApiResponse<BomDto>>;

public sealed class CreateBomCommandValidator : AbstractValidator<CreateBomCommand>
{
    public CreateBomCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.OutputQuantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A BOM must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemType).Must(t => t is "RawMaterial" or "Product")
                .WithMessage("Line item type must be RawMaterial or Product.");
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.WastagePercent).InclusiveBetween(0, 100);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => (l.ItemType, l.ItemId)).Distinct().Count() == lines.Count)
            .WithMessage("The same component appears more than once in the BOM lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateBomCommandHandler
    : IRequestHandler<CreateBomCommand, ApiResponse<BomDto>>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateBomCommandHandler(
        IRepository<Domain.Entities.Bom> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _productRepo = productRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<BomDto>> Handle(
        CreateBomCommand cmd, CancellationToken cancellationToken)
    {
        var product = await _productRepo.GetByIdAsync(cmd.ProductId, cancellationToken);
        if (product is null) return ApiResponse<BomDto>.Fail("Product not found.");

        var rawMaterialIds = cmd.Lines.Where(l => l.ItemType == "RawMaterial").Select(l => l.ItemId).Distinct().ToList();
        var componentProductIds = cmd.Lines.Where(l => l.ItemType == "Product").Select(l => l.ItemId).Distinct().ToList();

        // A BOM can't consume its own output product as a component (direct self-reference).
        if (componentProductIds.Contains(cmd.ProductId))
            return ApiResponse<BomDto>.Fail("A BOM cannot use its own output product as a component.");

        if (rawMaterialIds.Count > 0)
        {
            var rmCount = await _rawMaterialRepo.Query()
                .CountAsync(rm => rawMaterialIds.Contains(rm.Id), cancellationToken);
            if (rmCount != rawMaterialIds.Count)
                return ApiResponse<BomDto>.Fail("One or more raw materials not found.");
        }
        if (componentProductIds.Count > 0)
        {
            var cpCount = await _productRepo.Query()
                .CountAsync(p => componentProductIds.Contains(p.Id), cancellationToken);
            if (cpCount != componentProductIds.Count)
                return ApiResponse<BomDto>.Fail("One or more component products not found.");
        }

        // Version is monotonic per product — count soft-deleted versions too so numbers never repeat
        var maxVersion = await _repo.Query()
            .IgnoreQueryFilters()
            .Where(b => b.ProductId == cmd.ProductId)
            .Select(b => (int?)b.Version)
            .MaxAsync(cancellationToken) ?? 0;

        var code = await _numbering.NextAsync("BOM", null, cancellationToken);

        var entity = new Domain.Entities.Bom
        {
            Code = code,
            ProductId = cmd.ProductId,
            Version = maxVersion + 1,
            Name = string.IsNullOrWhiteSpace(cmd.Name) ? null : cmd.Name.Trim(),
            OutputQuantity = cmd.OutputQuantity,
            Status = Domain.Entities.BomStatus.Draft,
            IsActive = false,
            EffectiveDate = cmd.EffectiveDate,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.BomLine
            {
                RawMaterialId = l.ItemType == "RawMaterial" ? l.ItemId : null,
                ComponentProductId = l.ItemType == "Product" ? l.ItemId : null,
                Quantity = l.Quantity,
                WastagePercent = l.WastagePercent,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetBomByIdQuery(entity.Id), cancellationToken);
    }
}
