namespace BengalTex.ERP.Application.Subcontract.Dtos;

public sealed record SubcontractOrderDto(
    long Id,
    string Code,
    int SubcontractorId,
    string SubcontractorName,
    DateOnly OrderDate,
    DateOnly? ExpectedReturnDate,
    string ProcessType,
    int WarehouseId,
    string WarehouseName,
    string Status,
    decimal ChargeAmount,
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    DateTimeOffset? ReceivedAt,
    string? ReceivedBy,
    string? Notes,
    IReadOnlyList<SubcontractLineDto> Lines);

public sealed record SubcontractLineDto(
    long Id,
    int? RawMaterialId,
    int? ProductId,
    string ItemType,            // "RawMaterial" | "Product"
    string ItemCode,
    string ItemName,
    string UomCode,
    decimal IssuedQuantity,
    decimal ReceivedQuantity,
    int SortOrder,
    string? LineNotes);

public sealed record SubcontractOrderListItemDto(
    long Id,
    string Code,
    string SubcontractorName,
    DateOnly OrderDate,
    string ProcessType,
    string WarehouseName,
    string Status,
    int LineCount,
    decimal ChargeAmount);
