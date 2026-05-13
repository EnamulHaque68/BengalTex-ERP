using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Factory.Commands;

public sealed record DeleteFactoryCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteFactoryCommandHandler : IRequestHandler<DeleteFactoryCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Factory> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteFactoryCommandHandler(IRepository<Domain.Entities.Factory> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteFactoryCommand cmd, CancellationToken cancellationToken)
    {
        var factory = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (factory is null) return ApiResponse.Fail("Factory not found.");

        _repo.Remove(factory); // Soft-delete via AuditInterceptor
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Factory deleted.");
    }
}
