namespace BengalTex.ERP.Application.Style.Dtos;

public sealed record StyleDto(
    int Id,
    string Code,
    string StyleName,
    int BuyerId,
    string BuyerName,
    int? ProductId,
    string? ProductName,
    string? BuyerStyleRef,
    string? Season,
    string Status,
    string? Description,
    string? Notes,
    bool IsActive);

public sealed record StyleListItemDto(
    int Id,
    string Code,
    string StyleName,
    string BuyerName,
    string? ProductName,
    string? Season,
    string Status,
    bool IsActive);
