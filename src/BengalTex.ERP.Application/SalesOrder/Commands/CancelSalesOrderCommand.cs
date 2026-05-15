using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

/// <summary>Cancels a sales order from Draft or Confirmed. Lifecycle: * → Cancelled.</summary>
public sealed record CancelSalesOrderCommand(long Id) : IRequest<ApiResponse<SalesOrderDto>>;

internal sealed class CancelSalesOrderCommandHandler
    : IRequestHandler<CancelSalesOrderCommand, ApiResponse<SalesOrderDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CancelSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SalesOrderDto>> Handle(
        CancelSalesOrderCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (so is null) return ApiResponse<SalesOrderDto>.Fail("Sales order not found.");

        if (so.Status != Domain.Entities.SalesOrderStatus.Draft &&
            so.Status != Domain.Entities.SalesOrderStatus.Confirmed)
        {
            return ApiResponse<SalesOrderDto>.Fail("Sales order is already cancelled.");
        }

        so.Status = Domain.Entities.SalesOrderStatus.Cancelled;
        _repo.Update(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSalesOrderByIdQuery(so.Id), cancellationToken);
    }
}
