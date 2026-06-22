using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Commands;

// ── Create ──

public sealed record CreateOfficeLocationCommand(
    string Name, string Type, double Latitude, double Longitude, double RadiusMeters, string? Address, bool IsActive)
    : IRequest<ApiResponse<int>>;

public sealed class CreateOfficeLocationCommandValidator : AbstractValidator<CreateOfficeLocationCommand>
{
    public CreateOfficeLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<OfficeLocationType>(t, out _)).WithMessage("Invalid location type.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).InclusiveBetween(5, 100000);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

internal sealed class CreateOfficeLocationCommandHandler : IRequestHandler<CreateOfficeLocationCommand, ApiResponse<int>>
{
    private readonly IRepository<OfficeLocation> _repo;
    private readonly IRepository<Domain.Entities.Company> _companyRepo;
    private readonly IUnitOfWork _uow;

    public CreateOfficeLocationCommandHandler(IRepository<OfficeLocation> repo, IRepository<Domain.Entities.Company> companyRepo, IUnitOfWork uow)
    { _repo = repo; _companyRepo = companyRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateOfficeLocationCommand cmd, CancellationToken ct)
    {
        var companyId = await _companyRepo.Query().AsNoTracking().OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync(ct);
        if (companyId == 0) return ApiResponse<int>.Fail("No company is configured. Create a company first.");

        var entity = new OfficeLocation
        {
            CompanyId = companyId,
            Name = cmd.Name.Trim(),
            Type = Enum.Parse<OfficeLocationType>(cmd.Type),
            Latitude = cmd.Latitude,
            Longitude = cmd.Longitude,
            RadiusMeters = cmd.RadiusMeters,
            Address = string.IsNullOrWhiteSpace(cmd.Address) ? null : cmd.Address.Trim(),
            IsActive = cmd.IsActive
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(entity.Id, "Office location created.");
    }
}

// ── Update ──

public sealed record UpdateOfficeLocationCommand(
    int Id, string Name, string Type, double Latitude, double Longitude, double RadiusMeters, string? Address, bool IsActive)
    : IRequest<ApiResponse<int>>;

public sealed class UpdateOfficeLocationCommandValidator : AbstractValidator<UpdateOfficeLocationCommand>
{
    public UpdateOfficeLocationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<OfficeLocationType>(t, out _)).WithMessage("Invalid location type.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).InclusiveBetween(5, 100000);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

internal sealed class UpdateOfficeLocationCommandHandler : IRequestHandler<UpdateOfficeLocationCommand, ApiResponse<int>>
{
    private readonly IRepository<OfficeLocation> _repo;
    private readonly IUnitOfWork _uow;

    public UpdateOfficeLocationCommandHandler(IRepository<OfficeLocation> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateOfficeLocationCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.Query().FirstOrDefaultAsync(o => o.Id == cmd.Id, ct);
        if (entity is null) return ApiResponse<int>.Fail("Office location not found.");

        entity.Name = cmd.Name.Trim();
        entity.Type = Enum.Parse<OfficeLocationType>(cmd.Type);
        entity.Latitude = cmd.Latitude;
        entity.Longitude = cmd.Longitude;
        entity.RadiusMeters = cmd.RadiusMeters;
        entity.Address = string.IsNullOrWhiteSpace(cmd.Address) ? null : cmd.Address.Trim();
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(entity.Id, "Office location updated.");
    }
}

// ── Delete (soft) ──

public sealed record DeleteOfficeLocationCommand(int Id) : IRequest<ApiResponse<int>>;

internal sealed class DeleteOfficeLocationCommandHandler : IRequestHandler<DeleteOfficeLocationCommand, ApiResponse<int>>
{
    private readonly IRepository<OfficeLocation> _repo;
    private readonly IRepository<EmployeeOfficeLocation> _assignRepo;
    private readonly IUnitOfWork _uow;

    public DeleteOfficeLocationCommandHandler(
        IRepository<OfficeLocation> repo, IRepository<EmployeeOfficeLocation> assignRepo, IUnitOfWork uow)
    { _repo = repo; _assignRepo = assignRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(DeleteOfficeLocationCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.Query().FirstOrDefaultAsync(o => o.Id == cmd.Id, ct);
        if (entity is null) return ApiResponse<int>.Fail("Office location not found.");

        // Remove employee assignments so they don't dangle
        var assignments = await _assignRepo.Query().Where(a => a.OfficeLocationId == cmd.Id).ToListAsync(ct);
        foreach (var a in assignments) _assignRepo.Remove(a);

        _repo.Remove(entity);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(cmd.Id, "Office location deleted.");
    }
}

// ── Set authorized employees (replace the assignment set) ──

public sealed record SetOfficeLocationEmployeesCommand(int OfficeLocationId, IReadOnlyList<int> EmployeeIds)
    : IRequest<ApiResponse<int>>;

internal sealed class SetOfficeLocationEmployeesCommandHandler : IRequestHandler<SetOfficeLocationEmployeesCommand, ApiResponse<int>>
{
    private readonly IRepository<OfficeLocation> _repo;
    private readonly IRepository<EmployeeOfficeLocation> _assignRepo;
    private readonly IUnitOfWork _uow;

    public SetOfficeLocationEmployeesCommandHandler(
        IRepository<OfficeLocation> repo, IRepository<EmployeeOfficeLocation> assignRepo, IUnitOfWork uow)
    { _repo = repo; _assignRepo = assignRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(SetOfficeLocationEmployeesCommand cmd, CancellationToken ct)
    {
        var exists = await _repo.Query().AnyAsync(o => o.Id == cmd.OfficeLocationId, ct);
        if (!exists) return ApiResponse<int>.Fail("Office location not found.");

        var current = await _assignRepo.Query().Where(a => a.OfficeLocationId == cmd.OfficeLocationId).ToListAsync(ct);
        var currentIds = current.Select(a => a.EmployeeId).ToHashSet();
        var targetIds = cmd.EmployeeIds.Distinct().ToHashSet();

        foreach (var a in current.Where(a => !targetIds.Contains(a.EmployeeId)))
            _assignRepo.Remove(a);

        foreach (var id in targetIds.Where(id => !currentIds.Contains(id)))
            await _assignRepo.AddAsync(new EmployeeOfficeLocation { OfficeLocationId = cmd.OfficeLocationId, EmployeeId = id }, ct);

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(cmd.OfficeLocationId, $"{targetIds.Count} employee(s) authorized for this location.");
    }
}
