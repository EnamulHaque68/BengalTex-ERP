using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

// ── List all office locations (with assigned-employee counts) ──

public sealed record GetOfficeLocationsQuery : IRequest<ApiResponse<IReadOnlyList<OfficeLocationDto>>>;

internal sealed class GetOfficeLocationsQueryHandler
    : IRequestHandler<GetOfficeLocationsQuery, ApiResponse<IReadOnlyList<OfficeLocationDto>>>
{
    private readonly IRepository<OfficeLocation> _repo;
    private readonly IRepository<EmployeeOfficeLocation> _assignRepo;

    public GetOfficeLocationsQueryHandler(IRepository<OfficeLocation> repo, IRepository<EmployeeOfficeLocation> assignRepo)
    { _repo = repo; _assignRepo = assignRepo; }

    public async Task<ApiResponse<IReadOnlyList<OfficeLocationDto>>> Handle(GetOfficeLocationsQuery req, CancellationToken ct)
    {
        var counts = await _assignRepo.Query().AsNoTracking()
            .GroupBy(a => a.OfficeLocationId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var list = await _repo.Query().AsNoTracking().OrderBy(o => o.Name)
            .Select(o => new { o.Id, o.Name, o.Type, o.Latitude, o.Longitude, o.RadiusMeters, o.Address, o.IsActive })
            .ToListAsync(ct);

        var dtos = list.Select(o => new OfficeLocationDto(
            o.Id, o.Name, o.Type.ToString(), o.Latitude, o.Longitude, o.RadiusMeters, o.Address, o.IsActive,
            counts.TryGetValue(o.Id, out var c) ? c : 0)).ToList();

        return ApiResponse<IReadOnlyList<OfficeLocationDto>>.Ok(dtos);
    }
}

// ── Assignment picker: every active employee + whether assigned to this location ──

public sealed record GetOfficeLocationEmployeesQuery(int OfficeLocationId)
    : IRequest<ApiResponse<IReadOnlyList<OfficeLocationEmployeeDto>>>;

internal sealed class GetOfficeLocationEmployeesQueryHandler
    : IRequestHandler<GetOfficeLocationEmployeesQuery, ApiResponse<IReadOnlyList<OfficeLocationEmployeeDto>>>
{
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<EmployeeOfficeLocation> _assignRepo;

    public GetOfficeLocationEmployeesQueryHandler(
        IRepository<Domain.Entities.Employee> employeeRepo, IRepository<EmployeeOfficeLocation> assignRepo)
    { _employeeRepo = employeeRepo; _assignRepo = assignRepo; }

    public async Task<ApiResponse<IReadOnlyList<OfficeLocationEmployeeDto>>> Handle(
        GetOfficeLocationEmployeesQuery req, CancellationToken ct)
    {
        var assigned = await _assignRepo.Query().AsNoTracking()
            .Where(a => a.OfficeLocationId == req.OfficeLocationId)
            .Select(a => a.EmployeeId).ToListAsync(ct);
        var assignedSet = assigned.ToHashSet();

        var employees = await _employeeRepo.Query().AsNoTracking()
            .Where(e => e.IsActive && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.FullName)
            .Select(e => new { e.Id, e.Code, e.FullName, e.Designation, e.Department })
            .ToListAsync(ct);

        var dtos = employees.Select(e => new OfficeLocationEmployeeDto(
            e.Id, e.Code, e.FullName, e.Designation, e.Department, assignedSet.Contains(e.Id))).ToList();

        return ApiResponse<IReadOnlyList<OfficeLocationEmployeeDto>>.Ok(dtos);
    }
}
