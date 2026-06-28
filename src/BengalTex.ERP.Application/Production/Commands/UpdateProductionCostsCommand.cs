using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>
/// Records the manual cost components on a production order's cost sheet (Phase 6). Material cost is
/// auto-captured at Complete; this captures the actual labour / machine / overhead / subcontract /
/// wastage / reject costs (base BDT). Editable any time except once the order is Cancelled, so the
/// actuals can be entered after completion.
/// </summary>
public sealed record UpdateProductionCostsCommand(
    long Id,
    decimal LabourCost,
    decimal MachineCost,
    decimal OverheadCost,
    decimal SubcontractCost,
    decimal WastageCost,
    decimal RejectCost
) : IRequest<ApiResponse<ProductionOrderDto>>;

public sealed class UpdateProductionCostsCommandValidator : AbstractValidator<UpdateProductionCostsCommand>
{
    public UpdateProductionCostsCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.LabourCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MachineCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OverheadCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SubcontractCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WastageCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RejectCost).GreaterThanOrEqualTo(0);
    }
}

internal sealed class UpdateProductionCostsCommandHandler
    : IRequestHandler<UpdateProductionCostsCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateProductionCostsCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        UpdateProductionCostsCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");
        if (po.Status == Domain.Entities.ProductionOrderStatus.Cancelled)
            return ApiResponse<ProductionOrderDto>.Fail("Cannot edit the cost sheet of a cancelled production order.");

        po.LabourCost = cmd.LabourCost;
        po.MachineCost = cmd.MachineCost;
        po.OverheadCost = cmd.OverheadCost;
        po.SubcontractCost = cmd.SubcontractCost;
        po.WastageCost = cmd.WastageCost;
        po.RejectCost = cmd.RejectCost;
        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
