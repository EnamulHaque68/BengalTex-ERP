namespace BengalTex.ERP.Application.User.Dtos;

/// <summary>
/// Compact user representation for list/table views.
/// </summary>
public record UserListItemDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    int? FactoryId,
    bool IsActive,
    bool IsLockedOut,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);
