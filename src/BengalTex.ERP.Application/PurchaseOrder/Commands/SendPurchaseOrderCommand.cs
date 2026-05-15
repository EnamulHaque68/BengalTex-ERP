using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Application.PurchaseOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.PurchaseOrder.Commands;

/// <summary>Marks an approved PO as sent to the supplier. Lifecycle: Approved → Sent.</summary>
public sealed record SendPurchaseOrderCommand(long Id) : IRequest<ApiResponse<PurchaseOrderDto>>;

internal sealed class SendPurchaseOrderCommandHandler
    : IRequestHandler<SendPurchaseOrderCommand, ApiResponse<PurchaseOrderDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public SendPurchaseOrderCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PurchaseOrderDto>> Handle(
        SendPurchaseOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<PurchaseOrderDto>.Fail("Purchase order not found.");
        if (po.Status != Domain.Entities.PurchaseOrderStatus.Approved)
            return ApiResponse<PurchaseOrderDto>.Fail("Only approved purchase orders can be sent.");

        po.Status = Domain.Entities.PurchaseOrderStatus.Sent;
        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), cancellationToken);
    }
}
