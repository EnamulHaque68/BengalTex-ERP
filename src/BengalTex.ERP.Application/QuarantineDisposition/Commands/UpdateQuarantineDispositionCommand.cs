using BengalTex.ERP.Application.QuarantineDisposition.Dtos;
using BengalTex.ERP.Application.QuarantineDisposition.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.QuarantineDisposition.Commands;

/// <summary>
/// Updates a Draft quarantine disposition. DispositionType + quarantine warehouse are
/// fixed at create; date, destination (Release only), reason, notes, and lines can change.
/// Lines fully replaced. Posted dispositions are immutable.
/// </summary>
public sealed record UpdateQuarantineDispositionCommand(
    long Id,
    DateOnly DispositionDate,
    int? DestinationWarehouseId,
    string? Reason,
    string? Notes,
    IReadOnlyList<QuarantineDispositionLineInput> Lines
) : IRequest<ApiResponse<QuarantineDispositionDto>>;

public sealed class UpdateQuarantineDispositionCommandValidator : AbstractValidator<UpdateQuarantineDispositionCommand>
{
    public UpdateQuarantineDispositionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DispositionDate).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
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

internal sealed class UpdateQuarantineDispositionCommandHandler
    : IRequestHandler<UpdateQuarantineDispositionCommand, ApiResponse<QuarantineDispositionDto>>
{
    private readonly IRepository<Domain.Entities.QuarantineDisposition, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateQuarantineDispositionCommandHandler(
        IRepository<Domain.Entities.QuarantineDisposition, long> repo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<QuarantineDispositionDto>> Handle(
        UpdateQuarantineDispositionCommand cmd, CancellationToken cancellationToken)
    {
        var disp = await _repo.Query()
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == cmd.Id, cancellationToken);

        if (disp is null) return ApiResponse<QuarantineDispositionDto>.Fail("Quarantine disposition not found.");
        if (disp.Status != Domain.Entities.QuarantineDispositionStatus.Draft)
            return ApiResponse<QuarantineDispositionDto>.Fail("Only draft dispositions can be edited.");

        if (disp.DispositionType == Domain.Entities.DispositionType.Release)
        {
            if (!cmd.DestinationWarehouseId.HasValue)
                return ApiResponse<QuarantineDispositionDto>.Fail("A destination warehouse is required for a Release disposition.");
            if (cmd.DestinationWarehouseId.Value == disp.QuarantineWarehouseId)
                return ApiResponse<QuarantineDispositionDto>.Fail("Destination warehouse must differ from the quarantine warehouse.");
            var dest = await _warehouseRepo.GetByIdAsync(cmd.DestinationWarehouseId.Value, cancellationToken);
            if (dest is null) return ApiResponse<QuarantineDispositionDto>.Fail("Destination warehouse not found.");
            disp.DestinationWarehouseId = dest.Id;
        }

        disp.DispositionDate = cmd.DispositionDate;
        disp.Reason = cmd.Reason;
        disp.Notes = cmd.Notes;

        disp.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            disp.Lines.Add(new Domain.Entities.QuarantineDispositionLine
            {
                RawMaterialId = line.RawMaterialId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(disp);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetQuarantineDispositionByIdQuery(disp.Id), cancellationToken);
    }
}
