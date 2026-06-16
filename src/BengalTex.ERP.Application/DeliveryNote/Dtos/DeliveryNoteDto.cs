namespace BengalTex.ERP.Application.DeliveryNote.Dtos;

public record DeliveryNoteDto(
    long Id,
    string Code,
    long SalesOrderId,
    string SalesOrderCode,
    int CustomerId,
    string CustomerName,
    DateOnly DispatchDate,
    int DispatchWarehouseId,
    string DispatchWarehouseName,
    string Status,                       // enum as string
    DateOnly? PlannedDeliveryDate,
    string? VehicleNumber,
    string? TransportCompany,
    string? DriverName,
    string? DriverContact,
    string? DeliveryAddress,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    IReadOnlyList<DeliveryNoteLineDto> Lines);

public record DeliveryNoteLineDto(
    long Id,
    long SalesOrderLineId,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal OrderedQuantity,             // from SO line, for context
    decimal DispatchedQuantity,          // this DN line's quantity
    decimal ReturnedQuantity,            // already-returned via posted CRNs (Phase 13)
    int SortOrder,
    string? LineNotes);

public record DeliveryNoteListItemDto(
    long Id,
    string Code,
    long SalesOrderId,
    string SalesOrderCode,
    string CustomerName,
    DateOnly DispatchDate,
    string DispatchWarehouseCode,
    string Status,
    int LineCount);
