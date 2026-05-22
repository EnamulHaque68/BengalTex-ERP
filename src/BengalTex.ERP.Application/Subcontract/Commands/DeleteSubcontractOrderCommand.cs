using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Subcontract.Commands;

public sealed record DeleteSubcontractOrderCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteSubcontractOrderCommandHandler
    : IRequestHandler<DeleteSubcontractOrderCommand, ApiResponse>
{
    private readonly IRepository<SubcontractOrder, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteSubcontractOrderCommandHandler(IRepository<SubcontractOrder, long> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteSubcontractOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(cmd.Id, ct);
        if (order is null) return ApiResponse.Fail("Subcontract order not found.");
        if (order.Status is not (SubcontractStatus.Draft or SubcontractStatus.Cancelled))
            return ApiResponse.Fail("Only draft or cancelled subcontract orders can be deleted.");

        _repo.Remove(order);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Subcontract order deleted.");
    }
}
