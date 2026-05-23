namespace BengalTex.ERP.Application.Wastage.Dtos;

public record WastageReasonDto(int Id, string Name, bool IsReusable, bool IsActive, string? Description);

public record WastageEntryDto(
    long Id,
    string Code,
    DateOnly WastageDate,
    long? ProductionOrderId,
    string? ProductionOrderCode,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    int WastageReasonId,
    string WastageReasonName,
    bool IsReusable,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string? Department,
    string? Notes);

public record WastageEntryListItemDto(
    long Id,
    string Code,
    DateOnly WastageDate,
    string RawMaterialName,
    string WastageReasonName,
    bool IsReusable,
    decimal Quantity,
    decimal TotalCost,
    string? Department);

public record WastageSummaryRowDto(
    int WastageReasonId,
    string WastageReasonName,
    bool IsReusable,
    decimal TotalCost,
    int Count);

public record WastageSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<WastageSummaryRowDto> Rows,
    decimal TotalCost);
