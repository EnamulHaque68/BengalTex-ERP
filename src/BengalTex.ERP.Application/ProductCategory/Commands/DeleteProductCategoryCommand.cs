using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProductCategory.Commands;

public sealed record DeleteProductCategoryCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteProductCategoryCommandHandler
    : IRequestHandler<DeleteProductCategoryCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.ProductCategory> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteProductCategoryCommandHandler(
        IRepository<Domain.Entities.ProductCategory> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteProductCategoryCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Product category not found.");

        // Block delete if any product still references this category.
        var hasProducts = await _repo.Query()
            .Where(c => c.Id == cmd.Id)
            .AnyAsync(c => c.Products.Any(p => !p.IsDeleted), cancellationToken);
        if (hasProducts)
            return ApiResponse.Fail(
                $"Category '{entity.Name}' still has products. Reassign or remove them first.");

        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Product category deleted.");
    }
}
