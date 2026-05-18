using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>Marks a draft production order as in-progress and stamps the actual start date.</summary>
public sealed record StartProductionOrderCommand(long Id) : IRequest<ApiResponse<ProductionOrderDto>>;

internal sealed class StartProductionOrderCommandHandler
    : IRequestHandler<StartProductionOrderCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public StartProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        StartProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");
        if (po.Status != Domain.Entities.ProductionOrderStatus.Draft)
            return ApiResponse<ProductionOrderDto>.Fail("Only draft production orders can be started.");

        po.Status = Domain.Entities.ProductionOrderStatus.InProgress;
        po.ActualStartDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
