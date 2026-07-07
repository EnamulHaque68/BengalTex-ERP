using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Costing;

// ═══════════════════════════ Production cost sheet ═══════════════════════════

public sealed record ProductionCostSheetRowDto(
    long ProductionOrderId, string Code, string ProductName, string? StyleName,
    decimal Quantity, string Status,
    decimal MaterialCost, decimal LabourCost, decimal MachineCost, decimal OverheadCost, decimal SubcontractCost,
    decimal TotalCost, decimal UnitCost);

public sealed record ProductionCostSheetDto(
    DateOnly FromDate, DateOnly ToDate, IReadOnlyList<ProductionCostSheetRowDto> Rows,
    decimal TotalMaterial, decimal TotalLabour, decimal TotalMachine, decimal TotalOverhead, decimal TotalSubcontract, decimal GrandTotal);

/// <summary>Phase A4 — the fully-loaded cost sheet per completed production order for a period.</summary>
public sealed record GetProductionCostSheetQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<ProductionCostSheetDto>>;

internal sealed class GetProductionCostSheetQueryHandler
    : IRequestHandler<GetProductionCostSheetQuery, ApiResponse<ProductionCostSheetDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    public GetProductionCostSheetQueryHandler(IRepository<Domain.Entities.ProductionOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<ProductionCostSheetDto>> Handle(GetProductionCostSheetQuery q, CancellationToken ct)
    {
        var rows = await _repo.Query().AsNoTracking()
            .Where(p => p.Status == ProductionOrderStatus.Completed
                     && p.ActualEndDate >= q.FromDate && p.ActualEndDate <= q.ToDate)
            .OrderByDescending(p => p.ActualEndDate)
            .Select(p => new ProductionCostSheetRowDto(
                p.Id, p.Code, p.Product.Name, p.Style != null ? p.Style.StyleName : null,
                p.Quantity, p.Status.ToString(),
                p.MaterialCost, p.LabourCost, p.MachineCost, p.OverheadCost, p.SubcontractCost,
                p.MaterialCost + p.LabourCost + p.MachineCost + p.OverheadCost + p.SubcontractCost,
                p.Quantity > 0 ? (p.MaterialCost + p.LabourCost + p.MachineCost + p.OverheadCost + p.SubcontractCost) / p.Quantity : 0m))
            .ToListAsync(ct);

        return ApiResponse<ProductionCostSheetDto>.Ok(new ProductionCostSheetDto(
            q.FromDate, q.ToDate, rows,
            rows.Sum(r => r.MaterialCost), rows.Sum(r => r.LabourCost), rows.Sum(r => r.MachineCost),
            rows.Sum(r => r.OverheadCost), rows.Sum(r => r.SubcontractCost), rows.Sum(r => r.TotalCost)));
    }
}

// ═══════════════════════════ WIP report ═══════════════════════════

public sealed record WipReportRowDto(
    long ProductionOrderId, string Code, string ProductName, string? StyleName,
    decimal Quantity, decimal EstimatedValue, DateOnly? StartDate);

public sealed record WipReportDto(
    IReadOnlyList<WipReportRowDto> Rows, decimal TotalEstimatedValue, decimal GlWipBalance, decimal Variance);

/// <summary>Phase A4 — in-progress production orders with their estimated WIP value, tied to GL 1160.</summary>
public sealed record GetWipCostReportQuery(DateOnly? AsOfDate = null) : IRequest<ApiResponse<WipReportDto>>;

internal sealed class GetWipCostReportQueryHandler : IRequestHandler<GetWipCostReportQuery, ApiResponse<WipReportDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IRepository<JournalEntryLine, long> _lineRepo;

    public GetWipCostReportQueryHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo, IRepository<JournalEntryLine, long> lineRepo)
    {
        _repo = repo; _lineRepo = lineRepo;
    }

    public async Task<ApiResponse<WipReportDto>> Handle(GetWipCostReportQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var rows = await _repo.Query().AsNoTracking()
            .Where(p => p.Status == ProductionOrderStatus.InProgress)
            .OrderBy(p => p.Code)
            .Select(p => new WipReportRowDto(
                p.Id, p.Code, p.Product.Name, p.Style != null ? p.Style.StyleName : null,
                p.Quantity, Math.Round(p.Quantity * p.Product.WeightedAverageCost, 2), p.ActualStartDate))
            .ToListAsync(ct);

        // GL 1160 balance (Dr − Cr) as of the date.
        var glWip = await _lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= asOf
                     && l.Account.Code == Accounting.LedgerAccounts.WorkInProgressInventory)
            .Select(l => l.Debit - l.Credit).SumAsync(ct);
        glWip = Math.Round(glWip, 2);

        var totalEst = rows.Sum(r => r.EstimatedValue);
        return ApiResponse<WipReportDto>.Ok(new WipReportDto(rows, totalEst, glWip, Math.Round(totalEst - glWip, 2)));
    }
}
