namespace BengalTex.ERP.Application.GoodsReceipt.Dtos;

public record GoodsReceiptDto(
    long Id,
    string Code,
    long PurchaseOrderId,
    string PurchaseOrderCode,
    int SupplierId,
    string SupplierName,
    DateOnly ReceiveDate,
    int ReceivingWarehouseId,
    string ReceivingWarehouseName,
    string Status,                       // enum as string
    string? SupplierDeliveryRef,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    IReadOnlyList<GoodsReceiptLineDto> Lines);

public record GoodsReceiptLineDto(
    long Id,
    long PurchaseOrderLineId,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal OrderedQuantity,             // from PO line, for context
    decimal ReceivedQuantity,            // this GRN line's quantity
    int SortOrder,
    string? LineNotes);

public record GoodsReceiptListItemDto(
    long Id,
    string Code,
    long PurchaseOrderId,
    string PurchaseOrderCode,
    string SupplierName,
    DateOnly ReceiveDate,
    string ReceivingWarehouseCode,
    string Status,
    int LineCount);
