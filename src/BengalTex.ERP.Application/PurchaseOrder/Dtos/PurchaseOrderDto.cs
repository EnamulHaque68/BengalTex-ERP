namespace BengalTex.ERP.Application.PurchaseOrder.Dtos;

public record PurchaseOrderDto(
    long Id,
    string Code,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int? DeliveryWarehouseId,
    string? DeliveryWarehouseName,
    string Status,                       // enum as string
    DateTimeOffset? ApprovedAt,
    string? ApprovedBy,
    string? Notes,
    decimal TotalAmount,                 // Σ line totals
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public record PurchaseOrderLineDto(
    long Id,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,                   // Quantity * UnitPrice
    decimal ReceivedQuantity,
    int SortOrder,
    string? LineNotes);

public record PurchaseOrderListItemDto(
    long Id,
    string Code,
    int SupplierId,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string Status,
    int LineCount,
    decimal TotalAmount);
