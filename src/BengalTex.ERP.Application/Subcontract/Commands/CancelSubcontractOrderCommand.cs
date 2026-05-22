using BengalTex.ERP.Application.Subcontract.Dtos;
using BengalTex.ERP.Application.Subcontract.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Subcontract.Commands;

/// <summary>Cancels a Draft subcontract order (nothing has left stock yet).</summary>
public sealed record CancelSubcontractOrderCommand(long Id) : IRequest<ApiResponse<SubcontractOrderDto>>;

internal sealed class CancelSubcontractOrderCommandHandler
    : IRequestHandler<CancelSubcontractOrderCommand, ApiResponse<SubcontractOrderDto>>
{
    private readonly IRepository<SubcontractOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CancelSubcontractOrderCommandHandler(
        IRepository<SubcontractOrder, long> repo, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SubcontractOrderDto>> Handle(
        CancelSubcontractOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.Id, ct);
        if (order is null) return ApiResponse<SubcontractOrderDto>.Fail("Subcontract order not found.");
        if (order.Status != SubcontractStatus.Draft)
            return ApiResponse<SubcontractOrderDto>.Fail("Only draft subcontract orders can be cancelled (issued stock must be received back).");

        order.Status = SubcontractStatus.Cancelled;
        _repo.Update(order);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSubcontractOrderByIdQuery(order.Id), ct);
    }
}
