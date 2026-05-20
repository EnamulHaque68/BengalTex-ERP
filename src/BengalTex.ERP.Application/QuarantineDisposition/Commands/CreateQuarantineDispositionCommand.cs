using BengalTex.ERP.Application.QuarantineDisposition.Dtos;
using BengalTex.ERP.Application.QuarantineDisposition.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QuarantineDisposition.Commands;

/// <summary>One disposed line — an RM or Product currently in the quarantine warehouse.</summary>
public sealed record QuarantineDispositionLineInput(
    int? RawMaterialId,
    int? ProductId,
    decimal Quantity,
    string? LineNotes);

/// <summary>
/// Creates a Draft quarantine disposition. Type = Release (needs DestinationWarehouseId)
/// or Scrap (no destination). No stock movement yet — Posting applies it. Lines reference
/// items currently held in the quarantine warehouse.
/// </summary>
public sealed record CreateQuarantineDispositionCommand(
    string DispositionType,             // "Release" | "Scrap"
    DateOnly DispositionDate,
    int QuarantineWarehouseId,
    int? DestinationWarehouseId,
    string? Reason,
    string? Notes,
    IReadOnlyList<QuarantineDispositionLineInput> Lines
) : IRequest<ApiResponse<QuarantineDispositionDto>>;

public sealed class CreateQuarantineDispositionCommandValidator : AbstractValidator<CreateQuarantineDispositionCommand>
{
    public CreateQuarantineDispositionCommandValidator()
    {
        RuleFor(x => x.DispositionType).NotEmpty()
            .Must(t => t is "Release" or "Scrap")
            .WithMessage("DispositionType must be Release or Scrap.");
        RuleFor(x => x.QuarantineWarehouseId).GreaterThan(0);
        RuleFor(x => x.DispositionDate).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.DestinationWarehouseId).GreaterThan(0)
            .When(x => x.DispositionType == "Release")
            .WithMessage("A destination warehouse is required for a Release disposition.");
        RuleFor(x => x.DestinationWarehouseId).NotEqual(x => x.QuarantineWarehouseId)
            .When(x => x.DispositionType == "Release")
            .WithMessage("Destination warehouse must differ from the quarantine warehouse.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A disposition must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
            line.RuleFor(l => l)
                .Must(l => (l.RawMaterialId.HasValue && !l.ProductId.HasValue)
                        || (!l.RawMaterialId.HasValue && l.ProductId.HasValue))
                .WithMessage("Each line must have exactly one of RawMaterialId or ProductId.");
        });
    }
}

internal sealed class CreateQuarantineDispositionCommandHandler
    : IRequestHandler<CreateQuarantineDispositionCommand, ApiResponse<QuarantineDispositionDto>>
{
    private readonly IRepository<Domain.Entities.QuarantineDisposition, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateQuarantineDispositionCommandHandler(
        IRepository<Domain.Entities.QuarantineDisposition, long> repo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rmRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _rmRepo = rmRepo;
        _productRepo = productRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QuarantineDispositionDto>> Handle(
        CreateQuarantineDispositionCommand cmd, CancellationToken cancellationToken)
    {
        var type = Enum.Parse<Domain.Entities.DispositionType>(cmd.DispositionType);

        var quarantine = await _warehouseRepo.GetByIdAsync(cmd.QuarantineWarehouseId, cancellationToken);
        if (quarantine is null) return ApiResponse<QuarantineDispositionDto>.Fail("Quarantine warehouse not found.");

        int? destinationId = null;
        if (type == Domain.Entities.DispositionType.Release)
        {
            var dest = await _warehouseRepo.GetByIdAsync(cmd.DestinationWarehouseId!.Value, cancellationToken);
            if (dest is null) return ApiResponse<QuarantineDispositionDto>.Fail("Destination warehouse not found.");
            destinationId = dest.Id;
        }

        var rmIds = cmd.Lines.Where(l => l.RawMaterialId.HasValue).Select(l => l.RawMaterialId!.Value).Distinct().ToList();
        var productIds = cmd.Lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        if (rmIds.Count > 0)
        {
            var n = await _rmRepo.Query().CountAsync(rm => rmIds.Contains(rm.Id), cancellationToken);
            if (n != rmIds.Count) return ApiResponse<QuarantineDispositionDto>.Fail("One or more raw materials not found.");
        }
        if (productIds.Count > 0)
        {
            var n = await _productRepo.Query().CountAsync(p => productIds.Contains(p.Id), cancellationToken);
            if (n != productIds.Count) return ApiResponse<QuarantineDispositionDto>.Fail("One or more products not found.");
        }

        var code = await _numbering.NextAsync("DISP", null, cancellationToken);

        var entity = new Domain.Entities.QuarantineDisposition
        {
            Code = code,
            DispositionType = type,
            DispositionDate = cmd.DispositionDate,
            QuarantineWarehouseId = cmd.QuarantineWarehouseId,
            DestinationWarehouseId = destinationId,
            Status = Domain.Entities.QuarantineDispositionStatus.Draft,
            Reason = cmd.Reason,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.QuarantineDispositionLine
            {
                RawMaterialId = l.RawMaterialId,
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQuarantineDispositionByIdQuery(entity.Id), cancellationToken);
    }
}
