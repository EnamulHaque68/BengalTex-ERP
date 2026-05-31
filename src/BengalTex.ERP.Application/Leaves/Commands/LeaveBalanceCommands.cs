using BengalTex.ERP.Application.Leaves.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Leaves.Commands;

// ── List per year ──
public sealed record GetLeaveBalancesQuery(int Year, int? EmployeeId = null)
    : IRequest<ApiResponse<IReadOnlyList<LeaveBalanceDto>>>;

internal sealed class GetLeaveBalancesQueryHandler
    : IRequestHandler<GetLeaveBalancesQuery, ApiResponse<IReadOnlyList<LeaveBalanceDto>>>
{
    private readonly IRepository<LeaveBalance> _repo;
    public GetLeaveBalancesQueryHandler(IRepository<LeaveBalance> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<LeaveBalanceDto>>> Handle(GetLeaveBalancesQuery request, CancellationToken ct)
    {
        var q = _repo.Query().Where(b => b.Year == request.Year);
        if (request.EmployeeId.HasValue) q = q.Where(b => b.EmployeeId == request.EmployeeId.Value);
        var items = await q.OrderBy(b => b.Employee.FullName).ThenBy(b => b.LeaveType.Code)
            .Select(b => new LeaveBalanceDto(
                b.Id, b.EmployeeId, b.Employee.Code, b.Employee.FullName,
                b.LeaveTypeId, b.LeaveType.Code, b.LeaveType.Name,
                b.Year, b.Entitled, b.Taken, b.Entitled - b.Taken))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<LeaveBalanceDto>>.Ok(items);
    }
}

// ── Initialize Year ── (HR: ensures every active employee × active leave type has a balance row for Year)
public sealed record InitializeYearBalancesCommand(int Year) : IRequest<ApiResponse<int>>;

public sealed class InitializeYearBalancesCommandValidator : AbstractValidator<InitializeYearBalancesCommand>
{
    public InitializeYearBalancesCommandValidator() { RuleFor(x => x.Year).InclusiveBetween(2000, 2100); }
}

internal sealed class InitializeYearBalancesCommandHandler
    : IRequestHandler<InitializeYearBalancesCommand, ApiResponse<int>>
{
    private readonly IRepository<LeaveBalance> _repo;
    private readonly IRepository<LeaveType> _typeRepo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;

    public InitializeYearBalancesCommandHandler(
        IRepository<LeaveBalance> repo, IRepository<LeaveType> typeRepo,
        IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow)
    {
        _repo = repo; _typeRepo = typeRepo; _empRepo = empRepo; _uow = uow;
    }

    public async Task<ApiResponse<int>> Handle(InitializeYearBalancesCommand cmd, CancellationToken ct)
    {
        var types = await _typeRepo.Query().Where(t => t.IsActive && t.AnnualEntitlement > 0).ToListAsync(ct);
        var employees = await _empRepo.Query().Where(e => e.IsActive && e.Status == EmployeeStatus.Active).ToListAsync(ct);
        var existing = await _repo.Query().Where(b => b.Year == cmd.Year)
            .Select(b => new { b.EmployeeId, b.LeaveTypeId })
            .ToListAsync(ct);
        var existingSet = existing.Select(x => (x.EmployeeId, x.LeaveTypeId)).ToHashSet();

        var created = 0;
        foreach (var emp in employees)
            foreach (var t in types)
            {
                if (existingSet.Contains((emp.Id, t.Id))) continue;
                await _repo.AddAsync(new LeaveBalance
                {
                    EmployeeId = emp.Id, LeaveTypeId = t.Id,
                    Year = cmd.Year, Entitled = t.AnnualEntitlement, Taken = 0
                }, ct);
                created++;
            }
        if (created > 0) await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(created, $"{created} balance row(s) initialized for {cmd.Year}.");
    }
}

// ── Adjust ── (HR: override entitled / taken on a specific balance row)
public sealed record AdjustLeaveBalanceCommand(int Id, decimal Entitled, decimal Taken) : IRequest<ApiResponse>;

public sealed class AdjustLeaveBalanceCommandValidator : AbstractValidator<AdjustLeaveBalanceCommand>
{
    public AdjustLeaveBalanceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Entitled).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Taken).GreaterThanOrEqualTo(0);
    }
}

internal sealed class AdjustLeaveBalanceCommandHandler : IRequestHandler<AdjustLeaveBalanceCommand, ApiResponse>
{
    private readonly IRepository<LeaveBalance> _repo;
    private readonly IUnitOfWork _uow;
    public AdjustLeaveBalanceCommandHandler(IRepository<LeaveBalance> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(AdjustLeaveBalanceCommand cmd, CancellationToken ct)
    {
        var b = await _repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) return ApiResponse.Fail("Balance row not found.");
        b.Entitled = cmd.Entitled; b.Taken = cmd.Taken;
        _repo.Update(b);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Balance adjusted.");
    }
}
