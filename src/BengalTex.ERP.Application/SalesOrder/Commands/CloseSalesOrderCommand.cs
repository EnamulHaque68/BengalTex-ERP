using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

/// <summary>
/// Marks an SO as closed once dispatch is settled. Closure separates physical
/// dispatch (auto-set by DN postings) from final commercial settlement — a manual
/// decision that also accommodates short-ship scenarios (close on partial when both
/// parties agree the order is settled). Lifecycle:
/// PartiallyDispatched | Dispatched | Delivered → Closed.
///
/// Mirror of <see cref="ClosePurchaseOrderCommand"/> on the sales side.
/// </summary>
public sealed record CloseSalesOrderCommand(long Id) : IRequest<ApiResponse<SalesOrderDto>>;

internal sealed class CloseSalesOrderCommandHandler
    : IRequestHandler<CloseSalesOrderCommand, ApiResponse<SalesOrderDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CloseSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SalesOrderDto>> Handle(
        CloseSalesOrderCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (so is null) return ApiResponse<SalesOrderDto>.Fail("Sales order not found.");

        if (so.Status != Domain.Entities.SalesOrderStatus.PartiallyDispatched &&
            so.Status != Domain.Entities.SalesOrderStatus.Dispatched &&
            so.Status != Domain.Entities.SalesOrderStatus.Delivered)
        {
            return ApiResponse<SalesOrderDto>.Fail(
                "Sales order can only be closed once dispatch has started.");
        }

        so.Status = Domain.Entities.SalesOrderStatus.Closed;
        _repo.Update(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSalesOrderByIdQuery(so.Id), cancellationToken);
    }
}
