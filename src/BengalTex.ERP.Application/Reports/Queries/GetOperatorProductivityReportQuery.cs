using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Per-operator productivity for Job Cards in the date window. Cards count when their
/// `CompletedAt` (for Completed) or `StartedAt` (for InProgress) falls in range. Default
/// window = trailing 30 days. Aggregates: card count, completed/rejected qty, active
/// minutes, units-per-hour, reject %.
/// </summary>
public sealed record GetOperatorProductivityReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<OperatorProductivityReportDto>>;

internal sealed class GetOperatorProductivityReportQueryHandler
    : IRequestHandler<GetOperatorProductivityReportQuery, ApiResponse<OperatorProductivityReportDto>>
{
    private readonly IRepository<JobCard, long> _repo;
    public GetOperatorProductivityReportQueryHandler(IRepository<JobCard, long> repo) => _repo = repo;

    public async Task<ApiResponse<OperatorProductivityReportDto>> Handle(
        GetOperatorProductivityReportQuery req, CancellationToken ct)
    {
        var toDate = req.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = req.FromDate ?? toDate.AddDays(-30);
        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Pull cards with an assigned operator whose Completed or Started timestamp falls in window
        var cards = await _repo.Query()
            .Where(c => c.OperatorEmployeeId != null
                     && c.Status != JobCardStatus.Cancelled
                     && ((c.CompletedAt != null && c.CompletedAt >= fromUtc && c.CompletedAt < toUtc)
                       || (c.Status == JobCardStatus.InProgress && c.StartedAt != null && c.StartedAt >= fromUtc && c.StartedAt < toUtc)))
            .Select(c => new
            {
                c.OperatorEmployeeId,
                EmployeeCode = c.OperatorEmployee!.Code,
                EmployeeName = c.OperatorEmployee.FullName,
                Designation = c.OperatorEmployee.DesignationEntity != null
                    ? c.OperatorEmployee.DesignationEntity.Name
                    : c.OperatorEmployee.Designation,
                Department = c.OperatorEmployee.DepartmentEntity != null
                    ? c.OperatorEmployee.DepartmentEntity.Name
                    : c.OperatorEmployee.Department,
                c.Status,
                c.CompletedQuantity,
                c.RejectedQuantity,
                c.ActiveMinutes
            })
            .ToListAsync(ct);

        var grouped = cards
            .GroupBy(c => new { c.OperatorEmployeeId, c.EmployeeCode, c.EmployeeName, c.Designation, c.Department })
            .Select(g =>
            {
                var completedCards = g.Count(x => x.Status == JobCardStatus.Completed);
                var inProgressCards = g.Count(x => x.Status == JobCardStatus.InProgress);
                var totalCompleted = g.Sum(x => x.CompletedQuantity);
                var totalRejected = g.Sum(x => x.RejectedQuantity);
                var totalMinutes = g.Sum(x => x.ActiveMinutes ?? 0);
                var unitsPerHour = totalMinutes > 0
                    ? Math.Round((totalCompleted / totalMinutes) * 60m, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                var rejectRate = (totalCompleted + totalRejected) > 0
                    ? Math.Round(totalRejected / (totalCompleted + totalRejected) * 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                var efficiency = 100m - rejectRate;
                return new OperatorProductivityRowDto(
                    g.Key.OperatorEmployeeId!.Value, g.Key.EmployeeCode, g.Key.EmployeeName,
                    g.Key.Department, g.Key.Designation,
                    completedCards, inProgressCards,
                    totalCompleted, totalRejected, totalMinutes,
                    unitsPerHour, rejectRate, efficiency);
            })
            .OrderByDescending(r => r.TotalCompletedQuantity)
            .ToList();

        var totalMinutesGrand = grouped.Sum(r => r.TotalActiveMinutes);
        var totalCompletedGrand = grouped.Sum(r => r.TotalCompletedQuantity);
        var avgUph = totalMinutesGrand > 0
            ? Math.Round((totalCompletedGrand / totalMinutesGrand) * 60m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return ApiResponse<OperatorProductivityReportDto>.Ok(new OperatorProductivityReportDto(
            fromDate, toDate,
            grouped.Count,
            totalCompletedGrand,
            totalMinutesGrand,
            avgUph,
            grouped));
    }
}

// ─── Machine variant (same shape, grouped by Machine) ─────────────────────
public sealed record GetMachineProductivityReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<MachineProductivityReportDto>>;

internal sealed class GetMachineProductivityReportQueryHandler
    : IRequestHandler<GetMachineProductivityReportQuery, ApiResponse<MachineProductivityReportDto>>
{
    private readonly IRepository<JobCard, long> _repo;
    public GetMachineProductivityReportQueryHandler(IRepository<JobCard, long> repo) => _repo = repo;

    public async Task<ApiResponse<MachineProductivityReportDto>> Handle(
        GetMachineProductivityReportQuery req, CancellationToken ct)
    {
        var toDate = req.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = req.FromDate ?? toDate.AddDays(-30);
        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var cards = await _repo.Query()
            .Where(c => c.MachineId != null
                     && c.Status != JobCardStatus.Cancelled
                     && ((c.CompletedAt != null && c.CompletedAt >= fromUtc && c.CompletedAt < toUtc)
                       || (c.Status == JobCardStatus.InProgress && c.StartedAt != null && c.StartedAt >= fromUtc && c.StartedAt < toUtc)))
            .Select(c => new
            {
                c.MachineId,
                MachineCode = c.Machine!.Code,
                MachineName = c.Machine.Name,
                MachineType = c.Machine.MachineType,
                Location = c.Machine.Location,
                c.Status,
                c.CompletedQuantity,
                c.RejectedQuantity,
                c.ActiveMinutes
            })
            .ToListAsync(ct);

        var grouped = cards
            .GroupBy(c => new { c.MachineId, c.MachineCode, c.MachineName, c.MachineType, c.Location })
            .Select(g =>
            {
                var completedCards = g.Count(x => x.Status == JobCardStatus.Completed);
                var inProgressCards = g.Count(x => x.Status == JobCardStatus.InProgress);
                var totalCompleted = g.Sum(x => x.CompletedQuantity);
                var totalRejected = g.Sum(x => x.RejectedQuantity);
                var totalMinutes = g.Sum(x => x.ActiveMinutes ?? 0);
                var unitsPerHour = totalMinutes > 0
                    ? Math.Round((totalCompleted / totalMinutes) * 60m, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                var rejectRate = (totalCompleted + totalRejected) > 0
                    ? Math.Round(totalRejected / (totalCompleted + totalRejected) * 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                return new MachineProductivityRowDto(
                    g.Key.MachineId!.Value, g.Key.MachineCode, g.Key.MachineName,
                    g.Key.MachineType, g.Key.Location,
                    completedCards, inProgressCards,
                    totalCompleted, totalRejected, totalMinutes,
                    unitsPerHour, rejectRate);
            })
            .OrderByDescending(r => r.TotalCompletedQuantity)
            .ToList();

        var totalMinutesGrand = grouped.Sum(r => r.TotalActiveMinutes);
        var totalCompletedGrand = grouped.Sum(r => r.TotalCompletedQuantity);
        var avgUph = totalMinutesGrand > 0
            ? Math.Round((totalCompletedGrand / totalMinutesGrand) * 60m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return ApiResponse<MachineProductivityReportDto>.Ok(new MachineProductivityReportDto(
            fromDate, toDate,
            grouped.Count,
            totalCompletedGrand,
            totalMinutesGrand,
            avgUph,
            grouped));
    }
}
