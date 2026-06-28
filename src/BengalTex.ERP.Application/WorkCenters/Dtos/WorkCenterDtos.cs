namespace BengalTex.ERP.Application.WorkCenters.Dtos;

/// <summary>
/// A work center with its current planning load (Phase 4 — capacity). <see cref="PlannedLoad"/> is
/// the sum of planned quantities of the open production stages assigned to it; <see cref="LoadPercent"/>
/// compares that to <see cref="CapacityPerDay"/> (null when no capacity is configured).
/// </summary>
public record WorkCenterDto(
    int Id,
    string Code,
    string Name,
    string? Type,
    string? Location,
    decimal? CapacityPerDay,
    decimal? CostPerHour,
    string? Notes,
    bool IsActive,
    decimal PlannedLoad,
    int OpenStageCount,
    decimal? LoadPercent);
