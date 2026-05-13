using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Currency.Commands;

public sealed record DeleteCurrencyCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteCurrencyCommandHandler : IRequestHandler<DeleteCurrencyCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Currency> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteCurrencyCommandHandler(IRepository<Domain.Entities.Currency> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteCurrencyCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Currency not found.");

        if (entity.IsBaseCurrency)
            return ApiResponse.Fail("Base currency cannot be deleted. Promote another currency to base first.");

        _repo.Remove(entity); // Soft-delete via AuditInterceptor
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Currency deleted.");
    }
}
