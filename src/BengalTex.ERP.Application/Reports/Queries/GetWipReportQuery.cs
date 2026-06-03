using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Work-In-Progress snapshot of all <see cref="ProductionOrder"/>s currently in the
/// <see cref="ProductionOrderStatus.InProgress"/> state. Shows target quantity, days
/// running since ActualStartDate, overdue flag (against PlannedEndDate), and per-order
/// stage progress (X / Y stages completed, current stage name). Pure read-only.
/// </summary>
public sealed record GetWipReportQuery() : IRequest<ApiResponse<WipReportDto>>;

internal sealed class GetWipReportQueryHandler
    : IRequestHandler<GetWipReportQuery, ApiResponse<WipReportDto>>
{
    private readonly IRepository<ProductionOrder, long> _repo;
    public GetWipReportQueryHandler(IRepository<ProductionOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<WipReportDto>> Handle(GetWipReportQuery q, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var orders = await _repo.Query()
            .Where(p => p.Status == ProductionOrderStatus.InProgress)
            .OrderBy(p => p.ActualStartDate ?? p.PlannedStartDate ?? today)
            .Select(p => new
            {
                p.Id,
                PoCode = p.Code,
                p.ProductId,
                ProductCode = p.Product.Code,
                ProductName = p.Product.Name,
                Target = p.Quantity,
                p.PlannedStartDate, p.PlannedEndDate, p.ActualStartDate,
                IssueWarehouseName = p.IssueWarehouse.Name,
                ReceiveWarehouseName = p.ReceiveWarehouse.Name,
                Stages = p.Stages.OrderBy(s => s.Sequence).Select(s => new
                {
                    s.StageName,
                    s.Status
                }).ToList()
            })
            .ToListAsync(ct);

        var rows = orders.Select(o =>
        {
            var totalStages = o.Stages.Count;
            var completedStages = o.Stages.Count(s =>
                s.Status == ProductionStageStatus.Completed || s.Status == ProductionStageStatus.Skipped);
            var stageProgress = totalStages == 0 ? 0m
                : Math.Round((decimal)completedStages / totalStages * 100m, 1);
            var currentStage = o.Stages.FirstOrDefault(s =>
                s.Status != ProductionStageStatus.Completed && s.Status != ProductionStageStatus.Skipped)?.StageName;

            int daysRunning = 0;
            if (o.ActualStartDate.HasValue)
            {
                daysRunning = today.DayNumber - o.ActualStartDate.Value.DayNumber;
                if (daysRunning < 0) daysRunning = 0;
            }
            var isOverdue = o.PlannedEndDate.HasValue && today > o.PlannedEndDate.Value;

            return new WipReportRowDto(
                o.Id, o.PoCode, o.ProductId, o.ProductCode, o.ProductName,
                o.Target,
                o.PlannedStartDate, o.PlannedEndDate, o.ActualStartDate,
                daysRunning, isOverdue,
                totalStages, completedStages, stageProgress, currentStage,
                o.IssueWarehouseName, o.ReceiveWarehouseName);
        }).ToList();

        return ApiResponse<WipReportDto>.Ok(new WipReportDto(
            today,
            rows.Count,
            rows.Sum(r => r.TargetQuantity),
            rows));
    }
}
