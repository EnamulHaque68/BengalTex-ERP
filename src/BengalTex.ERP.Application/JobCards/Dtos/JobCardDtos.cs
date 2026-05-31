namespace BengalTex.ERP.Application.JobCards.Dtos;

public record JobCardDto(
    long Id,
    string Code,
    long ProductionOrderId,
    string ProductionOrderCode,
    string ProductName,
    long? ProductionStageId,
    string? StageName,
    string? BatchNumber,
    decimal Quantity,
    decimal CompletedQuantity,
    decimal RejectedQuantity,
    int? MachineId,
    string? MachineCode,
    string? MachineName,
    int? OperatorEmployeeId,
    string? OperatorCode,
    string? OperatorName,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? LastResumedAt,
    DateTimeOffset? CompletedAt,
    string? CompletedBy,
    int? ActiveMinutes,
    string? Notes,
    IReadOnlyList<JobCardScanDto> Scans);

public record JobCardListItemDto(
    long Id,
    string Code,
    long ProductionOrderId,
    string ProductionOrderCode,
    string ProductName,
    string? BatchNumber,
    decimal Quantity,
    decimal CompletedQuantity,
    decimal RejectedQuantity,
    string? MachineName,
    string? OperatorName,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? ActiveMinutes);

public record JobCardScanDto(
    long Id,
    string ScanType,
    DateTimeOffset ScannedAt,
    string? ScannedBy,
    decimal? Quantity,
    decimal? RejectedQuantity,
    string? Notes);

public record JobCardBoardCountsDto(int Open, int InProgress, int OnHold, int Completed, int Cancelled);
