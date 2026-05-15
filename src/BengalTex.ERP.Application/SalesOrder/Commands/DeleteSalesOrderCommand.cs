using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

public sealed record DeleteSalesOrderCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteSalesOrderCommandHandler
    : IRequestHandler<DeleteSalesOrderCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteSalesOrderCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (so is null) return ApiResponse.Fail("Sales order not found.");

        // Only Draft or Cancelled SOs may be deleted — Confirmed is a customer commitment worth preserving
        if (so.Status != Domain.Entities.SalesOrderStatus.Draft &&
            so.Status != Domain.Entities.SalesOrderStatus.Cancelled)
        {
            return ApiResponse.Fail("Only draft or cancelled sales orders can be deleted. Cancel it first.");
        }

        _repo.Remove(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Sales order deleted.");
    }
}
