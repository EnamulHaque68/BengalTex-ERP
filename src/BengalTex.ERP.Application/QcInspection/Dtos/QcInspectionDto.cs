namespace BengalTex.ERP.Application.QcInspection.Dtos;

public record QcInspectionDto(
    long Id,
    string Code,
    string SourceType,                  // "IncomingMaterial" | "FinishedGoods"
    long? GoodsReceiptNoteId,
    string? GoodsReceiptNoteCode,
    long? ProductionOrderId,
    string? ProductionOrderCode,
    string SourceLabel,                 // GRN/Production code + supplier/product for display
    DateOnly InspectionDate,
    int InspectedFromWarehouseId,
    string InspectedFromWarehouseName,
    int QuarantineWarehouseId,
    string QuarantineWarehouseName,
    string Status,
    string OverallResult,
    string? InspectedBy,
    string? Notes,
    IReadOnlyList<QcInspectionLineDto> Lines);

public record QcInspectionLineDto(
    long Id,
    string ItemType,                    // "RawMaterial" | "Product"
    int? RawMaterialId,
    int? ProductId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    decimal InspectedQuantity,
    decimal PassedQuantity,
    decimal RejectedQuantity,
    decimal AvailableAtSource,          // current stock on hand at the source warehouse
    int SortOrder,
    string? DefectNotes);

public record QcInspectionListItemDto(
    long Id,
    string Code,
    string SourceType,
    string SourceCode,
    DateOnly InspectionDate,
    string Status,
    string OverallResult,
    int LineCount,
    decimal TotalInspected,
    decimal TotalRejected);
