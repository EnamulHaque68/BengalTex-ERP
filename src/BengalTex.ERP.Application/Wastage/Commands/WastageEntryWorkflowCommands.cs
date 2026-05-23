using BengalTex.ERP.Application.Wastage.Dtos;
using BengalTex.ERP.Application.Wastage.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Wastage.Commands;

// ── Update ──
public sealed record UpdateWastageEntryCommand(
    long Id,
    DateOnly WastageDate,
    long? ProductionOrderId,
    int RawMaterialId,
    int WastageReasonId,
    decimal Quantity,
    string? Department,
    string? Notes
) : IRequest<ApiResponse<WastageEntryDto>>;

public sealed class UpdateWastageEntryCommandValidator : AbstractValidator<UpdateWastageEntryCommand>
{
    public UpdateWastageEntryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.WastageDate).NotEmpty();
        RuleFor(x => x.RawMaterialId).GreaterThan(0);
        RuleFor(x => x.WastageReasonId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Department).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateWastageEntryCommandHandler : IRequestHandler<UpdateWastageEntryCommand, ApiResponse<WastageEntryDto>>
{
    private readonly IRepository<WastageEntry, long> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateWastageEntryCommandHandler(
        IRepository<WastageEntry, long> repo, IRepository<Domain.Entities.RawMaterial> rmRepo, IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _rmRepo = rmRepo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<WastageEntryDto>> Handle(UpdateWastageEntryCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<WastageEntryDto>.Fail("Wastage entry not found.");
        var rm = await _rmRepo.GetByIdAsync(cmd.RawMaterialId, ct);
        if (rm is null) return ApiResponse<WastageEntryDto>.Fail("Raw material not found.");

        e.WastageDate = cmd.WastageDate;
        e.ProductionOrderId = cmd.ProductionOrderId;
        e.RawMaterialId = cmd.RawMaterialId;
        e.WastageReasonId = cmd.WastageReasonId;
        e.Quantity = cmd.Quantity;
        e.UnitCost = rm.WeightedAverageCost;
        e.TotalCost = Math.Round(cmd.Quantity * rm.WeightedAverageCost, 2, MidpointRounding.AwayFromZero);
        e.Department = string.IsNullOrWhiteSpace(cmd.Department) ? null : cmd.Department.Trim();
        e.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetWastageEntryByIdQuery(e.Id), ct);
    }
}

// ── Delete ──
public sealed record DeleteWastageEntryCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteWastageEntryCommandHandler : IRequestHandler<DeleteWastageEntryCommand, ApiResponse>
{
    private readonly IRepository<WastageEntry, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteWastageEntryCommandHandler(IRepository<WastageEntry, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteWastageEntryCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Wastage entry not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Wastage entry deleted.");
    }
}
