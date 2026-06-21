using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Employee.Commands;

/// <summary>Stores the storage path of an uploaded employee photo onto the employee (PhotoUrl).</summary>
public sealed record SetEmployeePhotoCommand(int EmployeeId, string StoragePath) : IRequest<ApiResponse<string>>;

internal sealed class SetEmployeePhotoCommandHandler : IRequestHandler<SetEmployeePhotoCommand, ApiResponse<string>>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    private readonly IUnitOfWork _uow;
    public SetEmployeePhotoCommandHandler(IRepository<Domain.Entities.Employee> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<string>> Handle(SetEmployeePhotoCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.EmployeeId, ct);
        if (e is null) return ApiResponse<string>.Fail("Employee not found.");
        e.PhotoUrl = cmd.StoragePath;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<string>.Ok(cmd.StoragePath, "Photo updated.");
    }
}

/// <summary>Returns the storage path of an employee's photo (null if none), for serving the image.</summary>
public sealed record GetEmployeePhotoPathQuery(int EmployeeId) : IRequest<string?>;

internal sealed class GetEmployeePhotoPathQueryHandler : IRequestHandler<GetEmployeePhotoPathQuery, string?>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    public GetEmployeePhotoPathQueryHandler(IRepository<Domain.Entities.Employee> repo) => _repo = repo;
    public async Task<string?> Handle(GetEmployeePhotoPathQuery req, CancellationToken ct)
        => (await _repo.GetByIdAsync(req.EmployeeId, ct))?.PhotoUrl;
}
