using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.Wastage.Dtos;
using BengalTex.ERP.Application.Wastage.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Wastage.Commands;

public sealed record CreateWastageEntryCommand(
    DateOnly WastageDate,
    long? ProductionOrderId,
    int RawMaterialId,
    int WastageReasonId,
    decimal Quantity,
    string? Department,
    string? Notes
) : IRequest<ApiResponse<WastageEntryDto>>;

public sealed class CreateWastageEntryCommandValidator : AbstractValidator<CreateWastageEntryCommand>
{
    public CreateWastageEntryCommandValidator()
    {
        RuleFor(x => x.WastageDate).NotEmpty();
        RuleFor(x => x.RawMaterialId).GreaterThan(0);
        RuleFor(x => x.WastageReasonId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Department).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateWastageEntryCommandHandler : IRequestHandler<CreateWastageEntryCommand, ApiResponse<WastageEntryDto>>
{
    private readonly IRepository<WastageEntry, long> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IRepository<WastageReason> _reasonRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateWastageEntryCommandHandler(
        IRepository<WastageEntry, long> repo,
        IRepository<Domain.Entities.RawMaterial> rmRepo,
        IRepository<WastageReason> reasonRepo,
        IUnitOfWork uow, INumberingService numbering, IMediator mediator)
    {
        _repo = repo;
        _rmRepo = rmRepo;
        _reasonRepo = reasonRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<WastageEntryDto>> Handle(CreateWastageEntryCommand cmd, CancellationToken ct)
    {
        var rm = await _rmRepo.GetByIdAsync(cmd.RawMaterialId, ct);
        if (rm is null) return ApiResponse<WastageEntryDto>.Fail("Raw material not found.");
        if (await _reasonRepo.GetByIdAsync(cmd.WastageReasonId, ct) is null)
            return ApiResponse<WastageEntryDto>.Fail("Wastage reason not found.");

        var unitCost = rm.WeightedAverageCost;
        var entity = new WastageEntry
        {
            Code = await _numbering.NextAsync("WST", null, ct),
            WastageDate = cmd.WastageDate,
            ProductionOrderId = cmd.ProductionOrderId,
            RawMaterialId = cmd.RawMaterialId,
            WastageReasonId = cmd.WastageReasonId,
            Quantity = cmd.Quantity,
            UnitCost = unitCost,
            TotalCost = Math.Round(cmd.Quantity * unitCost, 2, MidpointRounding.AwayFromZero),
            Department = string.IsNullOrWhiteSpace(cmd.Department) ? null : cmd.Department.Trim(),
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetWastageEntryByIdQuery(entity.Id), ct);
    }
}
