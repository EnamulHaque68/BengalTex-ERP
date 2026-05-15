using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

/// <summary>Confirms a draft SO — customer commitment locked in. Lifecycle: Draft → Confirmed.</summary>
public sealed record ConfirmSalesOrderCommand(long Id) : IRequest<ApiResponse<SalesOrderDto>>;

internal sealed class ConfirmSalesOrderCommandHandler
    : IRequestHandler<ConfirmSalesOrderCommand, ApiResponse<SalesOrderDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ConfirmSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SalesOrderDto>> Handle(
        ConfirmSalesOrderCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (so is null) return ApiResponse<SalesOrderDto>.Fail("Sales order not found.");
        if (so.Status != Domain.Entities.SalesOrderStatus.Draft)
            return ApiResponse<SalesOrderDto>.Fail("Only draft sales orders can be confirmed.");

        so.Status = Domain.Entities.SalesOrderStatus.Confirmed;
        so.ConfirmedAt = DateTimeOffset.UtcNow;
        so.ConfirmedBy = _currentUser.UserName;
        _repo.Update(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSalesOrderByIdQuery(so.Id), cancellationToken);
    }
}
