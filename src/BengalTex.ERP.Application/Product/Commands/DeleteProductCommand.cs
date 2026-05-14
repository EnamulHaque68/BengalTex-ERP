using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Product.Commands;

public sealed record DeleteProductCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Product> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteProductCommandHandler(IRepository<Domain.Entities.Product> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteProductCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Product not found.");

        // Future: block deletion when BOMs / Sales Orders / stock records reference this product.
        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Product deleted.");
    }
}
