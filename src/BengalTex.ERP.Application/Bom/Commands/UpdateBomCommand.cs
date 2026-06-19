using BengalTex.ERP.Application.Bom.Dtos;
using BengalTex.ERP.Application.Bom.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Bom.Commands;

public sealed record UpdateBomCommand(
    int Id,
    string? Name,
    decimal OutputQuantity,
    DateOnly? EffectiveDate,
    string? Notes,
    IReadOnlyList<BomLineInput> Lines
) : IRequest<ApiResponse<BomDto>>;

public sealed class UpdateBomCommandValidator : AbstractValidator<UpdateBomCommand>
{
    public UpdateBomCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateBomCommandHandler
    : IRequestHandler<UpdateBomCommand, ApiResponse<BomDto>>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateBomCommandHandler(
        IRepository<Domain.Entities.Bom> repo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _rawMaterialRepo = rawMaterialRepo;
        _productRepo = productRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<BomDto>> Handle(
        UpdateBomCommand cmd, CancellationToken cancellationToken)
    {
        var bom = await _repo.Query()
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == cmd.Id, cancellationToken);

        if (bom is null) return ApiResponse<BomDto>.Fail("BOM not found.");
        if (bom.Status != Domain.Entities.BomStatus.Draft)
            return ApiResponse<BomDto>.Fail("Only draft BOMs can be edited.");

        var rawMaterialIds = cmd.Lines.Where(l => l.ItemType == "RawMaterial").Select(l => l.ItemId).Distinct().ToList();
        var componentProductIds = cmd.Lines.Where(l => l.ItemType == "Product").Select(l => l.ItemId).Distinct().ToList();

        // A BOM can't consume its own output product as a component (direct self-reference).
        if (componentProductIds.Contains(bom.ProductId))
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

        bom.Name = string.IsNullOrWhiteSpace(cmd.Name) ? null : cmd.Name.Trim();
        bom.OutputQuantity = cmd.OutputQuantity;
        bom.EffectiveDate = cmd.EffectiveDate;
        bom.Notes = cmd.Notes;

        // Draft lines carry no history — replace the whole set
        bom.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            bom.Lines.Add(new Domain.Entities.BomLine
            {
                RawMaterialId = line.ItemType == "RawMaterial" ? line.ItemId : null,
                ComponentProductId = line.ItemType == "Product" ? line.ItemId : null,
                Quantity = line.Quantity,
                WastagePercent = line.WastagePercent,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(bom);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetBomByIdQuery(bom.Id), cancellationToken);
    }
}
