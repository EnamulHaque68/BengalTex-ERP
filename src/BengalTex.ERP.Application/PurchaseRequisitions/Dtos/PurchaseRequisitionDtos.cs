namespace BengalTex.ERP.Application.PurchaseRequisitions.Dtos;

public sealed record PurchaseRequisitionLineDto(
    long Id,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string? RawMaterialUnit,
    decimal Quantity,
    decimal EstimatedUnitPrice,
    decimal LineTotal,
    int SortOrder,
    string? LineNotes);

public sealed record PurchaseRequisitionDto(
    long Id,
    string Code,
    DateOnly RequisitionDate,
    DateOnly? NeededByDate,
    int? DepartmentId,
    string? DepartmentName,
    string? DepartmentText,
    string? RequestedBy,
    string? Purpose,
    string Status,
    decimal EstimatedTotal,
    DateTimeOffset? SubmittedAt,
    string? SubmittedByUser,
    DateTimeOffset? DecidedAt,
    string? DecidedByUser,
    string? DecisionNotes,
    DateTimeOffset? ConvertedAt,
    long? ConvertedPurchaseOrderId,
    string? ConvertedPurchaseOrderCode,
    string? Notes,
    IReadOnlyList<PurchaseRequisitionLineDto> Lines);
