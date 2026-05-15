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
            line.RuleFor(l => l.RawMaterialId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.WastagePercent).InclusiveBetween(0, 100);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("The same raw material appears more than once in the BOM lines.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class UpdateBomCommandHandler
    : IRequestHandler<UpdateBomCommand, ApiResponse<BomDto>>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateBomCommandHandler(
        IRepository<Domain.Entities.Bom> repo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _rawMaterialRepo = rawMaterialRepo;
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

        var rawMaterialIds = cmd.Lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var existingCount = await _rawMaterialRepo.Query()
            .CountAsync(rm => rawMaterialIds.Contains(rm.Id), cancellationToken);
        if (existingCount != rawMaterialIds.Count)
            return ApiResponse<BomDto>.Fail("One or more raw materials not found.");

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
                RawMaterialId = line.RawMaterialId,
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
