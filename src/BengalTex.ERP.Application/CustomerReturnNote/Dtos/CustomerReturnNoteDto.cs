namespace BengalTex.ERP.Application.CustomerReturnNote.Dtos;

public record CustomerReturnNoteDto(
    long Id,
    string Code,
    long DeliveryNoteId,
    string DeliveryNoteCode,
    long SalesOrderId,
    string SalesOrderCode,
    int CustomerId,
    string CustomerName,
    DateOnly ReturnDate,
    int ReturnWarehouseId,
    string ReturnWarehouseCode,
    string ReturnWarehouseName,
    string Status,
    string? VehicleNumber,
    string? Reason,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    IReadOnlyList<CustomerReturnNoteLineDto> Lines);

public record CustomerReturnNoteLineDto(
    long Id,
    long DeliveryNoteLineId,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal DispatchedQuantity,                 // from the source DN line
    decimal PreviouslyReturnedQuantity,         // sum of prior returns against this DN line (excluding this CRN)
    decimal ReturnedQuantity,                   // on this CRN
    decimal AvailableForReturn,                 // DispatchedQuantity − PreviouslyReturnedQuantity
    int SortOrder,
    string? LineNotes);

public record CustomerReturnNoteListItemDto(
    long Id,
    string Code,
    long DeliveryNoteId,
    string DeliveryNoteCode,
    int CustomerId,
    string CustomerName,
    DateOnly ReturnDate,
    int ReturnWarehouseId,
    string ReturnWarehouseName,
    string Status,
    int LineCount,
    decimal TotalReturnedQuantity);
