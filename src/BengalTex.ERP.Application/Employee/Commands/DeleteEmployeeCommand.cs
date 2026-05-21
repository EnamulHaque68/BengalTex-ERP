using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Employee.Commands;

/// <summary>Soft-deletes an employee (preserved via the AuditInterceptor).</summary>
public sealed record DeleteEmployeeCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteEmployeeCommandHandler
    : IRequestHandler<DeleteEmployeeCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteEmployeeCommandHandler(IRepository<Domain.Entities.Employee> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteEmployeeCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Employee not found.");

        _repo.Remove(entity);   // soft-delete via AuditInterceptor
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Employee deleted.");
    }
}
