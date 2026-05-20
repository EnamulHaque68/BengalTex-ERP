namespace BengalTex.ERP.Application.SupplierReturnNote.Dtos;

public record SupplierReturnNoteDto(
    long Id,
    string Code,
    long GoodsReceiptNoteId,
    string GoodsReceiptNoteCode,
    long PurchaseOrderId,
    string PurchaseOrderCode,
    int SupplierId,
    string SupplierName,
    DateOnly ReturnDate,
    int ReturnFromWarehouseId,
    string ReturnFromWarehouseCode,
    string ReturnFromWarehouseName,
    string Status,
    string? VehicleNumber,
    string? Reason,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    IReadOnlyList<SupplierReturnNoteLineDto> Lines);

public record SupplierReturnNoteLineDto(
    long Id,
    long GoodsReceiptLineId,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal ReceivedQuantity,                   // from the source GRN line
    decimal PreviouslyReturnedQuantity,
    decimal ReturnedQuantity,                   // on this SRN
    decimal AvailableForReturn,
    int SortOrder,
    string? LineNotes);

public record SupplierReturnNoteListItemDto(
    long Id,
    string Code,
    long GoodsReceiptNoteId,
    string GoodsReceiptNoteCode,
    int SupplierId,
    string SupplierName,
    DateOnly ReturnDate,
    int ReturnFromWarehouseId,
    string ReturnFromWarehouseName,
    string Status,
    int LineCount,
    decimal TotalReturnedQuantity);
