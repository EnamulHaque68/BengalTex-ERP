namespace BengalTex.ERP.Application.Samples.Dtos;

public record SampleDto(
    long Id,
    string Code,
    int CustomerId,
    string CustomerName,
    int? ProductId,
    string? ProductName,
    int? StyleId,
    string? StyleName,
    string Title,
    string? Description,
    string? BuyerReference,
    decimal Quantity,
    DateOnly RequestedDate,
    DateOnly? TargetDate,
    string Status,
    DateOnly? SubmittedDate,
    DateTimeOffset? DecidedAt,
    string? DecidedBy,
    string? Feedback,
    int? LeadTimeDays,
    string? Notes);

public record SampleListItemDto(
    long Id,
    string Code,
    string CustomerName,
    string Title,
    string? ProductName,
    decimal Quantity,
    DateOnly RequestedDate,
    DateOnly? TargetDate,
    string Status,
    int? LeadTimeDays);
